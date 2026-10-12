using NINA.Core.Enum;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Analysis {

    public sealed class RgbHfrResult {
        public double HfrR { get; set; } = double.NaN;
        public double HfrG { get; set; } = double.NaN;
        public double HfrB { get; set; } = double.NaN;
        /// <summary>HFR de la luminancia debayerizada por NINA (equivale al HFR global de NINA con "Debayered HFR" activo).</summary>
        public double HfrL { get; set; } = double.NaN;

        public int StarsR { get; set; }
        public int StarsG { get; set; }
        public int StarsB { get; set; }
        public int StarsL { get; set; }

        /// <summary>Número de estrellas detectadas en la luminancia (se usa como indicador general).</summary>
        public int StarsDetected => StarsL;

        // Detalle por canal (estadísticas, estrellas y vista previa)
        public ChannelDetail R { get; set; }
        public ChannelDetail G { get; set; }
        public ChannelDetail B { get; set; }
        public ChannelDetail L { get; set; }
    }

    /// <summary>
    /// Calcula el HFR de R, G, B y luminancia usando exactamente el mismo camino que NINA:
    /// debayer de NINA -> imagen monocroma por canal -> stretch con los ajustes del perfil ->
    /// IStarDetection.Detect con la sensibilidad y reducción de ruido del perfil.
    /// </summary>
    public sealed class RgbHfrAnalyzer {
        private readonly IProfileService profileService;
        private readonly IImageDataFactory imageDataFactory;
        private readonly IPluggableBehaviorSelector<IStarDetection> starDetectionSelector;
        private readonly ICameraMediator cameraMediator;

        // Detector integrado de NINA. Se usa siempre para el panel de análisis: si el usuario (o el autoenfoque)
        // ha seleccionado el detector de este plugin, no debe llamarse a sí mismo.
        private readonly StarDetection builtIn = new StarDetection();

        // Canales de la última imagen analizada, para poder mezclarlos bajo demanda sin repetir el debayer
        private ushort[] cachedR, cachedG, cachedB;
        private IImageData cachedSource;

        public RgbHfrAnalyzer(
            IProfileService profileService,
            IImageDataFactory imageDataFactory,
            IPluggableBehaviorSelector<IStarDetection> starDetectionSelector,
            ICameraMediator cameraMediator) {
            this.profileService = profileService;
            this.imageDataFactory = imageDataFactory;
            this.starDetectionSelector = starDetectionSelector;
            this.cameraMediator = cameraMediator;
        }

        public async Task<RgbHfrResult> AnalyzeAsync(IImageData image, CancellationToken token) {
            var result = new RgbHfrResult();

            var pattern = ResolveBayerPattern(image);

            // Mismo debayer que usa NINA (con los canales R, G, B y Lum guardados)
            var rendered = image.RenderImage();
            var debayered = await Task.Run(() => rendered.Debayer(saveColorChannels: true, saveLumChannel: true, bayerPattern: pattern), token);
            var data = debayered.DebayeredData;
            if (data == null) {
                return result;
            }

            // Se guardan solo las referencias a R, G y B (la luminancia se libera al terminar)
            cachedR = data.Red;
            cachedG = data.Green;
            cachedB = data.Blue;
            cachedSource = image;

            // Se procesa de uno en uno para no multiplicar el uso de memoria
            result.L = await DetectGlobalAsync(debayered, image, token);
            result.R = await DetectAsync("R", data.Red, image, token);
            result.G = await DetectAsync("G", data.Green, image, token);
            result.B = await DetectAsync("B", data.Blue, image, token);

            result.HfrL = result.L.Hfr; result.StarsL = result.L.Stars;
            result.HfrR = result.R.Hfr; result.StarsR = result.R.Stars;
            result.HfrG = result.G.Hfr; result.StarsG = result.G.Stars;
            result.HfrB = result.B.Hfr; result.StarsB = result.B.Stars;
            return result;
        }

        /// <summary>
        /// Analiza una mezcla de canales de la última imagen (por ejemplo "RG", "RB" o "GB"): promedia los canales
        /// elegidos y pasa el resultado por el mismo proceso de detección. Devuelve null si no hay datos.
        /// </summary>
        public async Task<ChannelDetail> AnalyzeMixAsync(string channels, CancellationToken token) {
            var source = cachedSource;
            var arrays = new List<ushort[]>();
            foreach (char c in channels) {
                ushort[] a = c == 'R' ? cachedR : c == 'G' ? cachedG : c == 'B' ? cachedB : null;
                if (a == null) return null;
                arrays.Add(a);
            }
            if (source == null || arrays.Count < 2) return null;

            var mixed = await Task.Run(() => Mix(arrays), token);
            return await DetectAsync(string.Join("+", channels.ToCharArray()), mixed, source, token);
        }

        private static ushort[] Mix(List<ushort[]> arrays) {
            int n = arrays[0].Length;
            var result = new ushort[n];
            if (arrays.Count == 2) {
                var a = arrays[0]; var b = arrays[1];
                for (int i = 0; i < n; i++) result[i] = (ushort)((a[i] + b[i]) / 2);
            } else {
                var a = arrays[0]; var b = arrays[1]; var c = arrays[2];
                for (int i = 0; i < n; i++) result[i] = (ushort)((a[i] + b[i] + c[i]) / 3);
            }
            return result;
        }

        private IStarDetection GetDetector() {
            var selected = starDetectionSelector?.GetBehavior();
            if (selected == null || selected.ContentId == typeof(AntonioAguadoRovira.NINA.Rgbfocus.Autofocus.RgbChannelStarDetection).FullName) {
                return builtIn;
            }
            return selected;
        }

        /// <summary>
        /// Detecta estrellas en un solo canal (R, G o B) de la imagen que entrega el autoenfoque, con el algoritmo de NINA.
        /// Es el mismo proceso que usa el panel de análisis: debayer de NINA, canal a imagen monocroma, stretch del perfil y detección.
        /// </summary>
        public async Task<StarDetectionResult> DetectChannelAsync(IRenderedImage rendered, string channelKey, StarDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token) {
            var raw = rendered.RawImageData;
            var pattern = ResolveBayerPattern(raw);

            var debayered = await Task.Run(() => raw.RenderImage().Debayer(saveColorChannels: true, saveLumChannel: false, bayerPattern: pattern), token);
            var data = debayered.DebayeredData;
            ushort[] channel;
            if (data == null) channel = null;
            else if (channelKey == "R+G") channel = Mix(new List<ushort[]> { data.Red, data.Green });
            else if (channelKey == "G+B") channel = Mix(new List<ushort[]> { data.Green, data.Blue });
            else if (channelKey == "R+B") channel = Mix(new List<ushort[]> { data.Red, data.Blue });
            else channel = channelKey == "R" ? data.Red : channelKey == "B" ? data.Blue : data.Green;
            if (channel == null || channel.Length == 0) {
                return await builtIn.Detect(rendered, rendered.Image.Format, p, progress, token);
            }

            var props = raw.Properties;
            var mono = imageDataFactory.CreateBaseImageData(channel, props.Width, props.Height, props.BitDepth, false, raw.MetaData);
            var settings = profileService.ActiveProfile.ImageSettings;
            var stretched = await mono.RenderImage().Stretch(settings.AutoStretchFactor, settings.BlackClipping, false);

            return await builtIn.Detect(stretched, stretched.Image.Format, p, progress, token);
        }

        private async Task<ChannelDetail> DetectAsync(string name, ushort[] channel, IImageData source, CancellationToken token) {
            if (channel == null || channel.Length == 0) return new ChannelDetail { Name = name };

            var settings = profileService.ActiveProfile.ImageSettings;
            var props = source.Properties;

            // Imagen monocroma con los datos de este canal (se reutilizan los metadatos originales:
            // NINA los usa para la escala de imagen en la detección de alta sensibilidad)
            var mono = imageDataFactory.CreateBaseImageData(channel, props.Width, props.Height, props.BitDepth, false, source.MetaData);

            var monoRendered = mono.RenderImage();
            var stretched = await monoRendered.Stretch(settings.AutoStretchFactor, settings.BlackClipping, false);

            var detector = GetDetector();
            var r = await detector.Detect(stretched, stretched.Image.Format, BuildParams(), new Progress<ApplicationStatus>(), token);
            return ChannelDetail.From(name, r, stretched.Image, props.Width, props.Height);
        }

        /// <summary>
        /// Global: exactamente el mismo camino que NINA para la imagen principal:
        /// imagen debayerizada en color -> stretch (enlazado o no según el perfil) -> detección.
        /// </summary>
        private async Task<ChannelDetail> DetectGlobalAsync(IDebayeredImage debayered, IImageData source, CancellationToken token) {
            var settings = profileService.ActiveProfile.ImageSettings;
            var stretched = await debayered.Stretch(settings.AutoStretchFactor, settings.BlackClipping, settings.UnlinkedStretch);
            var detector = GetDetector();
            var r = await detector.Detect(stretched, stretched.Image.Format, BuildParams(), new Progress<ApplicationStatus>(), token);
            return ChannelDetail.From("RGB", r, stretched.Image, source.Properties.Width, source.Properties.Height);
        }

        /// <summary>Mismos parámetros que NINA usa en RenderedImage.DetectStars (incluye los recortes y nº de estrellas del perfil de enfoque).</summary>
        private StarDetectionParams BuildParams() {
            var settings = profileService.ActiveProfile.ImageSettings;
            var focuser = profileService.ActiveProfile.FocuserSettings;

            var p = new StarDetectionParams {
                IsAutoFocus = false,
                Sensitivity = settings.StarSensitivity,
                NoiseReduction = settings.NoiseReduction
            };
            if (focuser.AutoFocusInnerCropRatio < 1) {
                p.InnerCropRatio = focuser.AutoFocusInnerCropRatio;
                p.UseROI = true;
            }
            if (focuser.AutoFocusOuterCropRatio < 1) {
                p.OuterCropRatio = focuser.AutoFocusOuterCropRatio;
                p.UseROI = true;
            }
            if (focuser.AutoFocusUseBrightestStars > 0) {
                p.NumberOfAFStars = focuser.AutoFocusUseBrightestStars;
            }
            return p;
        }

        /// <summary>Misma lógica que usa NINA al preparar la imagen para decidir el patrón Bayer.</summary>
        private SensorType ResolveBayerPattern(IImageData image) {
            SensorType pattern;
            int offsetX;
            int offsetY;

            var configured = profileService.ActiveProfile.CameraSettings.BayerPattern;
            var cam = image.MetaData?.Camera;

            if (configured != BayerPatternEnum.Auto && configured != BayerPatternEnum.None) {
                pattern = (SensorType)configured;
                offsetX = 0;
                offsetY = 0;
            } else if (cam != null && cam.SensorType != SensorType.Monochrome && cam.SensorType != SensorType.Color) {
                pattern = cam.SensorType;
                offsetX = cam.BayerOffsetX;
                offsetY = cam.BayerOffsetY;
            } else {
                var info = cameraMediator.GetInfo();
                pattern = info.SensorType;
                offsetX = info.BayerOffsetX;
                offsetY = info.BayerOffsetY;
            }

            if (pattern == SensorType.Monochrome || pattern == SensorType.Color) {
                pattern = SensorType.RGGB;
            }

            return ApplyOffsets(pattern, offsetX, offsetY);
        }

        /// <summary>
        /// Copia de BayerPatternUtility.ApplyOffsets de NINA (rama develop), que aún no existe en NINA 3.2.
        /// Un offset impar en X o en Y desplaza el patrón Bayer.
        /// </summary>
        private static SensorType ApplyOffsets(SensorType bayerPattern, int offsetX, int offsetY) {
            if (offsetX % 2 != 0) {
                bayerPattern = bayerPattern switch {
                    SensorType.RGGB => SensorType.GRBG,
                    SensorType.GRBG => SensorType.RGGB,
                    SensorType.GBRG => SensorType.BGGR,
                    SensorType.BGGR => SensorType.GBRG,
                    SensorType.GRGB => SensorType.RGBG,
                    SensorType.RGBG => SensorType.GRGB,
                    SensorType.GBGR => SensorType.BGRG,
                    SensorType.BGRG => SensorType.GBGR,
                    _ => bayerPattern
                };
            }

            if (offsetY % 2 != 0) {
                bayerPattern = bayerPattern switch {
                    SensorType.RGGB => SensorType.GBRG,
                    SensorType.GBRG => SensorType.RGGB,
                    SensorType.GRBG => SensorType.BGGR,
                    SensorType.BGGR => SensorType.GRBG,
                    SensorType.GRGB => SensorType.GBGR,
                    SensorType.GBGR => SensorType.GRGB,
                    SensorType.RGBG => SensorType.BGRG,
                    SensorType.BGRG => SensorType.RGBG,
                    _ => bayerPattern
                };
            }

            return bayerPattern;
        }
    }
}

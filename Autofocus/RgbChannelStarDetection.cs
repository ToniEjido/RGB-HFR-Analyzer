using AntonioAguadoRovira.NINA.Rgbfocus.Analysis;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Image.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Detector de estrellas que aparece en Opciones > Imagen > Detector de estrellas.
    /// Mide el HFR del canal activo (R, G o B) con el propio algoritmo de NINA; si el canal activo es RGB,
    /// o la imagen no es de color, delega directamente en el detector de NINA.
    /// El panel "RGB Autoenfoque" lo selecciona solo mientras dura el autoenfoque. Fuera del autoenfoque se comporta como el de NINA.
    /// </summary>
    [Export(typeof(IPluggableBehavior))]
    public class RgbChannelStarDetection : IStarDetection {
        private readonly RgbHfrAnalyzer analyzer;
        private readonly StarDetection inner = new StarDetection();

        [ImportingConstructor]
        public RgbChannelStarDetection(IProfileService profileService, IImageDataFactory imageDataFactory, ICameraMediator cameraMediator) {
            // Sin selector: este detector forma parte del selector y no debe depender de él
            analyzer = new RgbHfrAnalyzer(profileService, imageDataFactory, null, cameraMediator);
            Logger.Info("RGB HFR Analyzer: star detector 'RGB HFR Analyzer (active channel)' created");
        }

        public string Name => "RGB HFR Analyzer (active channel)";

        public string ContentId => GetType().FullName;

        public async Task<StarDetectionResult> Detect(IRenderedImage image, System.Windows.Media.PixelFormat pf, StarDetectionParams p, IProgress<ApplicationStatus> progress, CancellationToken token) {
            string channel = FocusChannelState.ActiveChannel;
            var raw = image?.RawImageData;

            // Solo se mide un canal durante el autoenfoque (p.IsAutoFocus). Para el resto de usos, como el HFR que NINA
            // muestra de cada imagen, se usa siempre el detector normal sobre la imagen completa.
            if (raw == null || !raw.Properties.IsBayered || channel == "L" || p == null || !p.IsAutoFocus) {
                return await inner.Detect(image, pf, p, progress, token);
            }
            return await analyzer.DetectChannelAsync(image, channel, p, progress, token);
        }

        public IStarDetectionAnalysis CreateAnalysis() => inner.CreateAnalysis();

        public void UpdateAnalysis(IStarDetectionAnalysis analysis, StarDetectionParams p, StarDetectionResult result) =>
            inner.UpdateAnalysis(analysis, p, result);
    }
}

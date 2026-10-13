using AntonioAguadoRovira.NINA.Rgbfocus.Analysis;
using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Core.Model;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusDockables {

    /// <summary>
    /// Aspecto de un botón de canal. El botón conserva siempre su estilo normal de NINA; cuando está
    /// seleccionado se le superpone el color de fondo "seleccionado" del tema (el mismo que usa NINA para
    /// un botón marcado) y su texto pasa al primer plano claro de los botones.
    /// </summary>
    public class ChannelButtonVisual : INotifyPropertyChanged {
        private readonly Brush normalLetter;
        private readonly Brush selectedLetter;
        private Brush selectionBrush = Brushes.Transparent;
        private Brush valueForeground = Brushes.LightGray;
        private Brush letterForeground = Brushes.LightGray;

        public ChannelButtonVisual(Brush normalLetter, Brush selectedLetter) {
            this.normalLetter = normalLetter;
            this.selectedLetter = selectedLetter;
            Update(false);
        }

        /// <summary>Capa que se pinta sobre el botón: transparente si no está seleccionado.</summary>
        public Brush SelectionBrush { get => selectionBrush; private set { selectionBrush = value; Raise(nameof(SelectionBrush)); } }
        public Brush ValueForeground { get => valueForeground; private set { valueForeground = value; Raise(nameof(ValueForeground)); } }
        public Brush LetterForeground { get => letterForeground; private set { letterForeground = value; Raise(nameof(LetterForeground)); } }

        public void Update(bool selected) {
            var normalText = ThemeBrush("PrimaryBrush", Brushes.LightGray);
            if (selected) {
                var light = ThemeBrush("ButtonForegroundBrush", Brushes.White);
                SelectionBrush = ThemeBrush("ButtonBackgroundSelectedBrush", new SolidColorBrush(Color.FromRgb(0x3D, 0xB8, 0xA8)));
                ValueForeground = light;
                LetterForeground = selectedLetter ?? light;
            } else {
                SelectionBrush = Brushes.Transparent;
                ValueForeground = normalText;
                LetterForeground = normalLetter ?? normalText;
            }
        }

        private static Brush ThemeBrush(string key, Brush fallback) =>
            Application.Current?.TryFindResource(key) as Brush ?? fallback;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Una celda del mapa de HFR por zonas, dibujada sobre la imagen. Los tamaños están en coordenadas de la imagen
    /// y se recalculan con el zoom para que el texto y las líneas midan siempre lo mismo en pantalla.
    /// </summary>
    public class ZoneCell : INotifyPropertyChanged {
        private double textSize = 15;
        private double subTextSize = 10;
        private Thickness lineThickness = new Thickness(1.5);
        private Thickness textPadding = new Thickness(5, 1, 5, 1);

        public string Text { get; set; } = "—";
        public string Count { get; set; } = string.Empty;
        public Brush Background { get; set; }
        public string ToolTip { get; set; } = string.Empty;

        public double TextSize { get => textSize; private set { textSize = value; Raise(nameof(TextSize)); } }
        public double SubTextSize { get => subTextSize; private set { subTextSize = value; Raise(nameof(SubTextSize)); } }
        public Thickness LineThickness { get => lineThickness; private set { lineThickness = value; Raise(nameof(LineThickness)); } }
        public Thickness TextPadding { get => textPadding; private set { textPadding = value; Raise(nameof(TextPadding)); } }

        /// <param name="displayScale">Escala con la que se muestra la imagen (zoom / escala de la vista previa).</param>
        public void ApplyScale(double displayScale) {
            double k = displayScale > 0 ? 1.0 / displayScale : 1.0;
            TextSize = 15 * k;
            SubTextSize = 10 * k;
            LineThickness = new Thickness(1.5 * k);
            TextPadding = new Thickness(5 * k, 1 * k, 5 * k, 1 * k);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>Comando mínimo para los botones de canal (evita depender de clases de NINA que cambian entre versiones).</summary>
    public class ActionCommand : ICommand {
        private readonly Action<object> execute;
        public ActionCommand(Action<object> execute) { this.execute = execute; }
        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => execute(parameter);
    }

    /// <summary>
    /// Panel del tab Imaging que muestra el HFR de los canales R, G y B de cada imagen que se guarda.
    /// </summary>
    [Export(typeof(IDockableVM))]
    public class RgbfocusDockable : DockableVM, IDisposable {
        private readonly IImagingMediator imagingMediator;
        private WeakReference<IImageData> lastAnalyzed;
        private readonly RgbHfrAnalyzer analyzer;
        private readonly IProfileService appProfileService;
        private int analyzing;

        [ImportingConstructor]
        public RgbfocusDockable(
            IProfileService profileService,
            IImagingMediator imagingMediator,
            IImageDataFactory imageDataFactory,
            IPluggableBehaviorSelector<IStarDetection> starDetectionSelector,
            ICameraMediator cameraMediator) : base(profileService) {
            this.appProfileService = profileService;
            this.analyzer = new RgbHfrAnalyzer(profileService, imageDataFactory, starDetectionSelector, cameraMediator);
            ImageGeometry = IconFactory.Create("RGB", "HFR");

            Title = Loc.T("Panel.Title");
            SelectChannelCommand = new ActionCommand(p => SelectChannel(p as string));
            UpdateChannelVisuals();
            ResetDiagnostics();

            this.imagingMediator = imagingMediator;
            this.imagingMediator.ImagePrepared += ImagingMediator_ImagePrepared;
            Logger.Info("RGB HFR Analyzer: analysis panel created");
        }

        public void Dispose() {
            imagingMediator.ImagePrepared -= ImagingMediator_ImagePrepared;
        }

        private string hfrR = "—";
        public string HfrR { get => hfrR; private set { hfrR = value; RaisePropertyChanged(); } }

        private string hfrG = "—";
        public string HfrG { get => hfrG; private set { hfrG = value; RaisePropertyChanged(); } }

        private string hfrB = "—";
        public string HfrB { get => hfrB; private set { hfrB = value; RaisePropertyChanged(); } }

        private string starsR = "—";
        public string StarsR { get => starsR; private set { starsR = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TipR)); } }

        private string starsG = "—";
        public string StarsG { get => starsG; private set { starsG = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TipG)); } }

        private string starsB = "—";
        public string StarsB { get => starsB; private set { starsB = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TipB)); } }

        private string starsL = "—";
        public string StarsL { get => starsL; private set { starsL = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(TipL)); } }

        private string hfrL = "—";
        public string HfrL { get => hfrL; private set { hfrL = value; RaisePropertyChanged(); } }

        // ---- Visor del canal seleccionado ----
        private readonly Dictionary<string, ChannelDetail> details = new Dictionary<string, ChannelDetail>();

        public ICommand SelectChannelCommand { get; }

        private static Brush Rgb(byte r, byte g, byte b) {
            var br = new SolidColorBrush(Color.FromRgb(r, g, b));
            br.Freeze();
            return br;
        }

        // Aspecto de cada botón de canal (el seleccionado se ve más claro, como en NINA)
        public ChannelButtonVisual VisR { get; } = new ChannelButtonVisual(Rgb(0xFF, 0x55, 0x55), Rgb(0xFF, 0x9A, 0xA3));
        public ChannelButtonVisual VisG { get; } = new ChannelButtonVisual(Rgb(0x55, 0xDD, 0x55), Rgb(0xA0, 0xF5, 0xA0));
        public ChannelButtonVisual VisB { get; } = new ChannelButtonVisual(Rgb(0x66, 0x99, 0xFF), Rgb(0xA8, 0xC4, 0xFF));
        public ChannelButtonVisual VisL { get; } = new ChannelButtonVisual(null, null);

        // Botones on/off: mismo aspecto que los de canal
        public ChannelButtonVisual VisWarn { get; } = new ChannelButtonVisual(null, null);
        public ChannelButtonVisual VisZones { get; } = new ChannelButtonVisual(null, null);
        public ChannelButtonVisual VisStars { get; } = new ChannelButtonVisual(null, null);
        public ChannelButtonVisual VisMix { get; } = new ChannelButtonVisual(null, null);

        // Textos emergentes de los botones de canal (número de estrellas detectadas)
        public string TipR => Loc.T("Tip.Channel", "R", starsR);
        public string TipG => Loc.T("Tip.Channel", "G", starsG);
        public string TipB => Loc.T("Tip.Channel", "B", starsB);
        public string TipL => Loc.T("Tip.Channel", "RGB", starsL);

        public ICommand ToggleWarningsCommand => new ActionCommand(_ => ShowWarnings = !ShowWarnings);
        public ICommand ToggleZonesCommand => new ActionCommand(_ => ShowZones = !ShowZones);
        public ICommand ToggleStarsCommand => new ActionCommand(_ => ShowStars = !ShowStars);
        public ICommand ToggleMixCommand => new ActionCommand(_ => MixChannels = !MixChannels);

        private void UpdateChannelVisuals() {
            // Los botones R, G y B se marcan si su canal está en la selección; RGB, solo si están los tres
            VisR.Update(selection.Contains('R'));
            VisG.Update(selection.Contains('G'));
            VisB.Update(selection.Contains('B'));
            VisL.Update(selection.Count == 3);
        }

        // ---- Selección de canales: uno, dos o los tres a la vez ----
        private readonly System.Collections.Generic.HashSet<char> selection = new System.Collections.Generic.HashSet<char> { 'R', 'G', 'B' };

        // Clave de la selección actual: "R", "G", "B" (uno), "RG", "RB", "GB" (mezcla de dos) o "L" (los tres, RGB de NINA).
        // Se guarda como cadena para poder leerla desde cualquier hilo.
        private volatile string selectionKey = "L";
        private string selectedChannel => selectionKey;
        private bool IsMixKey(string key) => key.Length > 1 && key != "L";
        private string selectedChannelName = "RGB";
        public string SelectedChannelName { get => selectedChannelName; private set { selectedChannelName = value; RaisePropertyChanged(); } }

        private bool showStars = false;
        public bool ShowStars { get => showStars; set { showStars = value; VisStars.Update(value); RaisePropertyChanged(); UpdateDisplay(); } }

        // Zoom relativo a la imagen ORIGINAL (1.0 = 100 %, un píxel de pantalla por píxel de sensor).
        // La vista previa está reducida, así que la escala real que se aplica a la vista previa es Zoom / PreviewScale.
        private const double MinZoom = 0.02;
        private const double MaxZoom = 8.0;
        private double zoom = 0.2;
        private bool autoFit = true;
        private double viewportWidth;
        private double viewportHeight;

        public double Zoom { get => zoom; private set { zoom = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(DisplayScale)); RaisePropertyChanged(nameof(ZoomText)); ApplyZoneScale(); } }
        public string ZoomText => $"{zoom * 100:0} %";

        /// <summary>Escala que se aplica al bitmap mostrado.</summary>
        public double DisplayScale {
            get {
                double ps = CurrentDetail()?.PreviewScale ?? 1.0;
                return ps > 0 ? zoom / ps : zoom;
            }
        }

        private ChannelDetail CurrentDetail() {
            details.TryGetValue(selectedChannel, out var d);
            return d;
        }

        /// <summary>Lo llama la vista cuando cambia el tamaño del visor.</summary>
        public void SetViewport(double width, double height) {
            viewportWidth = width;
            viewportHeight = height;
            if (autoFit) ApplyFit();
        }

        /// <summary>Zoom manual (botones, rueda del ratón). Desactiva el ajuste automático a la ventana.</summary>
        public void ZoomTo(double newZoom) {
            autoFit = false;
            Zoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));
        }

        public void FitToWindow() {
            autoFit = true;
            ApplyFit();
        }

        private void ApplyFit() {
            var d = CurrentDetail();
            if (d?.Preview == null || d.PreviewScale <= 0 || viewportWidth <= 0 || viewportHeight <= 0) return;
            double originalWidth = d.Preview.PixelWidth / d.PreviewScale;
            double originalHeight = d.Preview.PixelHeight / d.PreviewScale;
            Zoom = Math.Max(MinZoom, Math.Min(MaxZoom, Math.Min(viewportWidth / originalWidth, viewportHeight / originalHeight)));
        }

        private BitmapSource displayImage;
        public BitmapSource DisplayImage { get => displayImage; private set { displayImage = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(NoImageVisibility)); } }

        // ---- Mapa de zonas (superpuesto a la imagen), resumen numérico y avisos ----
        public ObservableCollection<ZoneCell> ZoneCells { get; } = new ObservableCollection<ZoneCell>();

        private bool showZones = false;
        public bool ShowZones { get => showZones; set { showZones = value; VisZones.Update(value); RaisePropertyChanged(); RaisePropertyChanged(nameof(ZoneOverlayVisibility)); } }
        public Visibility ZoneOverlayVisibility => showZones ? Visibility.Visible : Visibility.Collapsed;

        private string warnings = string.Empty;
        public string Warnings {
            get => warnings;
            private set {
                warnings = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(WarningVisibility));
                RaisePropertyChanged(nameof(WarningsButtonText));
            }
        }

        private bool showWarnings = false;
        /// <summary>Botón "Avisos": por defecto oculto, para dejar sitio a la imagen.</summary>
        public bool ShowWarnings { get => showWarnings; set { showWarnings = value; VisWarn.Update(value); RaisePropertyChanged(); RaisePropertyChanged(nameof(WarningVisibility)); } }

        /// <summary>El recuadro rojo solo se ve si el botón está activado y hay algo importante que comunicar.</summary>
        public Visibility WarningVisibility => showWarnings && !string.IsNullOrEmpty(warnings) ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>"Avisos" o "Avisos (n)" cuando hay avisos pendientes.</summary>
        public string WarningsButtonText {
            get {
                int n = string.IsNullOrEmpty(warnings) ? 0 : warnings.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
                return n == 0 ? Loc.T("Btn.Warnings") : Loc.T("Btn.WarningsN", n);
            }
        }

        private bool mixChannels = false;
        /// <summary>Con "Mezclar" desactivado cada botón selecciona un solo canal; activado, se pueden marcar varios a la vez.</summary>
        public bool MixChannels {
            get => mixChannels;
            set {
                mixChannels = value;
                VisMix.Update(value);
                RaisePropertyChanged();
                if (!value && selection.Count > 1 && selection.Count < 3) {
                    // Se vuelve a un solo canal (el primero en orden R, G, B)
                    char keep = "RGB".First(selection.Contains);
                    selection.Clear();
                    selection.Add(keep);
                    ApplySelection();
                }
            }
        }

        // Texto emergente con toda la información de la imagen mostrada
        private string infoText = string.Empty;
        public string InfoText { get => infoText; private set { infoText = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasInfo)); } }
        public bool HasInfo => !string.IsNullOrEmpty(infoText);

        public Visibility NoImageVisibility => displayImage == null ? Visibility.Visible : Visibility.Collapsed;

        private string diagnostics = string.Empty;
        public string Diagnostics { get => diagnostics; private set { diagnostics = value; RaisePropertyChanged(); } }

        private static readonly Color NeutralZone = Color.FromRgb(0x3A, 0x3F, 0x4B);
        private static readonly Color SharperZone = Color.FromRgb(0x2E, 0x6F, 0xD8);
        private static readonly Color SofterZone = Color.FromRgb(0xD9, 0x57, 0x3A);

        /// <summary>
        /// Color translúcido de una zona: azul = más nítida que la mediana global, rojo = menos nítida, gris si es igual.
        /// El color llega a su máximo con una diferencia del 10 %; se usa la raíz para que se noten también las diferencias pequeñas.
        /// </summary>
        private static Brush ZoneBrush(double zoneHfr, double overallHfr) {
            Color c = Color.FromArgb(0x50, NeutralZone.R, NeutralZone.G, NeutralZone.B);
            if (!double.IsNaN(zoneHfr) && overallHfr > 0) {
                double t = Math.Max(-1.0, Math.Min(1.0, (zoneHfr - overallHfr) / overallHfr / 0.10));
                var target = t < 0 ? SharperZone : SofterZone;
                double k = Math.Sqrt(Math.Abs(t));
                c = Color.FromArgb(
                    (byte)(0x50 + 0x80 * k),
                    (byte)(NeutralZone.R + (target.R - NeutralZone.R) * k),
                    (byte)(NeutralZone.G + (target.G - NeutralZone.G) * k),
                    (byte)(NeutralZone.B + (target.B - NeutralZone.B) * k));
            }
            var brush = new SolidColorBrush(c);
            brush.Freeze();
            return brush;
        }

        private static string Signed(double v) =>
            double.IsNaN(v) ? "—" : v.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);

        private void ApplyZoneScale() {
            double scale = DisplayScale;
            foreach (var cell in ZoneCells) cell.ApplyScale(scale);
        }

        private void ResetDiagnostics() {
            ZoneCells.Clear();
            for (int i = 0; i < ChannelDetail.ZoneGrid * ChannelDetail.ZoneGrid; i++) {
                ZoneCells.Add(new ZoneCell { Background = ZoneBrush(double.NaN, 0) });
            }
            ApplyZoneScale();
            Diagnostics = string.Empty;
        }

        private void UpdateDiagnostics(ChannelDetail d) {
            ZoneCells.Clear();
            double overall = double.IsNaN(d.MedianHfr) ? d.Hfr : d.MedianHfr;

            string[] names = {
                Loc.T("Zone.TL"), Loc.T("Zone.T"), Loc.T("Zone.TR"),
                Loc.T("Zone.L"), Loc.T("Zone.C"), Loc.T("Zone.R"),
                Loc.T("Zone.BL"), Loc.T("Zone.B"), Loc.T("Zone.BR")
            };

            for (int i = 0; i < ChannelDetail.ZoneGrid * ChannelDetail.ZoneGrid; i++) {
                double z = d.ZoneHfr[i];
                string tip;
                if (double.IsNaN(z)) {
                    tip = Loc.T("Zone.Few", names[i], d.ZoneStars[i]);
                } else {
                    double sd = d.ZoneStdDev[i];
                    double se = sd / Math.Sqrt(d.ZoneStars[i]);
                    tip = Loc.T("Zone.Tip",
                        names[i], SelectedChannelName,
                        Fmt(z), Fmt(d.ZoneMean[i]),
                        Fmt(sd), (sd / d.ZoneMean[i] * 100).ToString("0", CultureInfo.InvariantCulture),
                        se.ToString("0.000", CultureInfo.InvariantCulture),
                        d.ZoneStars[i],
                        Fmt(overall), ((z - overall) / overall * 100).ToString("+0;-0;0", CultureInfo.InvariantCulture));
                }

                ZoneCells.Add(new ZoneCell {
                    Text = Fmt(z),
                    Count = d.ZoneStars[i] > 0 ? $"{d.ZoneStars[i]} ★" : string.Empty,
                    Background = ZoneBrush(z, overall),
                    ToolTip = tip
                });
            }
            ApplyZoneScale();

            string rel = double.IsNaN(d.RelativeSigma) ? "—" : $"{d.RelativeSigma * 100:0} %";
            string err = double.IsNaN(d.StdError) ? "—" : $"±{d.StdError.ToString("0.000", CultureInfo.InvariantCulture)}";
            string range = double.IsNaN(d.ZoneMin) || overall <= 0
                ? "—"
                : $"{Fmt(d.ZoneMin)}–{Fmt(d.ZoneMax)} (Δ {(d.ZoneMax - d.ZoneMin) / overall * 100:0} %)";

            // Líneas para el cuadro emergente
            Diagnostics =
                Loc.T("Diag.RelSigma", rel) + "\n" +
                Loc.T("Diag.StdError", err, d.ValidStars) + "\n" +
                Loc.T("Diag.Zones", range) + "\n" +
                Loc.T("Diag.Tilt", Signed(d.TiltX), Signed(d.TiltY)) + "\n" +
                Loc.T("Diag.Curvature", Signed(d.Curvature));
        }

        private void SelectChannel(string key) {
            if (string.IsNullOrEmpty(key)) return;

            if (key == "L") {
                // RGB: se seleccionan los tres canales, con "Mezclar" activado o no
                selection.Clear();
                selection.Add('R'); selection.Add('G'); selection.Add('B');
            } else if (!mixChannels) {
                // Modo normal: un solo canal
                selection.Clear();
                selection.Add(key[0]);
            } else {
                // Con "Mezclar": cada botón es un interruptor; siempre debe quedar al menos un canal
                char c = key[0];
                if (selection.Contains(c)) {
                    if (selection.Count > 1) selection.Remove(c);
                } else {
                    selection.Add(c);
                }
            }
            ApplySelection();
        }

        private void ApplySelection() {
            selectionKey = selection.Count == 3 ? "L" : string.Concat("RGB".Where(selection.Contains));
            SelectedChannelName = selectionKey == "L" ? "RGB" : string.Join("+", selectionKey.ToCharArray());
            UpdateChannelVisuals();
            UpdateDisplay();
            _ = EnsureMixAsync();
        }

        private int analysisGeneration;
        private int mixBusy;
        private string captureStatus = string.Empty;

        /// <summary>
        /// Si la selección es una mezcla de dos canales y aún no se ha calculado para la imagen actual, la calcula
        /// (se mezclan los canales y se pasa por el detector de NINA). Debe llamarse desde el hilo de UI.
        /// </summary>
        private async Task EnsureMixAsync() {
            if (System.Threading.Interlocked.Exchange(ref mixBusy, 1) == 1) return; // ya hay un cálculo en curso: al acabar revisa la selección
            try {
                while (true) {
                    string key = selectedChannel;
                    if (!IsMixKey(key) || details.ContainsKey(key)) break;

                    int generation = analysisGeneration;
                    Status = Loc.T("Status.Mixing", string.Join("+", key.ToCharArray()));
                    var detail = await Task.Run(() => analyzer.AnalyzeMixAsync(key, System.Threading.CancellationToken.None));

                    if (generation != analysisGeneration) continue; // ha llegado otra imagen mientras tanto
                    if (detail == null) { Status = captureStatus; break; }

                    details[key] = detail;
                    Status = captureStatus;
                    UpdateDisplay();
                }
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: error calculating the channel mix: {ex}");
                Status = captureStatus;
            } finally {
                System.Threading.Interlocked.Exchange(ref mixBusy, 0);
            }
        }

        /// <summary>Fecha y hora de la captura, con el formato del idioma configurado en NINA.</summary>
        private string FormatCaptureTime(IImageData image) {
            var start = image?.MetaData?.Image?.ExposureStart ?? DateTime.MinValue;
            if (start <= DateTime.MinValue.AddDays(1)) return Loc.T("Info.CaptureUnknown");
            var culture = appProfileService?.ActiveProfile?.ApplicationSettings?.Language ?? CultureInfo.CurrentCulture;
            return Loc.T("Info.Capture", start.ToLocalTime().ToString("G", culture));
        }

        private void SetDetails(RgbHfrResult result) {
            analysisGeneration++;
            details.Clear(); // las mezclas de la imagen anterior ya no valen
            details["R"] = result.R;
            details["G"] = result.G;
            details["B"] = result.B;
            details["L"] = result.L;
            UpdateDisplay();
        }

        private void UpdateDisplay() {
            if (IsMixKey(selectedChannel) && !details.ContainsKey(selectedChannel) && details.ContainsKey("L")) {
                // Mezcla pendiente de calcular: se mantiene lo que había en pantalla hasta que esté lista
                InfoText = Loc.T("Info.Calculating", SelectedChannelName);
                return;
            }

            if (!details.TryGetValue(selectedChannel, out var d) || d == null || d.Preview == null) {
                DisplayImage = null;
                InfoText = string.Empty;
                ResetDiagnostics();
                Warnings = string.Empty;
                return;
            }

            UpdateDiagnostics(d);
            details.TryGetValue("R", out var dr);
            details.TryGetValue("G", out var dg);
            details.TryGetValue("B", out var db);
            details.TryGetValue("L", out var dl);
            Warnings = ResultInterpreter.Build(dr, dg, db, dl);
            InfoText = BuildInfo(d);
            DisplayImage = ShowStars ? d.RenderAnnotated() : d.Preview;
            if (autoFit) ApplyFit();
            RaisePropertyChanged(nameof(DisplayScale));
        }

        /// <summary>Todo el detalle de la imagen mostrada, para el cuadro emergente.</summary>
        private string BuildInfo(ChannelDetail d) {
            var sb = new System.Text.StringBuilder();
            if (!string.IsNullOrEmpty(captureStatus)) sb.AppendLine(captureStatus);
            sb.AppendLine(Loc.T("Info.Showing", SelectedChannelName));
            sb.AppendLine(Loc.T("Info.Hfr", Fmt(d.Hfr), Fmt(d.MedianHfr), Fmt(d.HfrStdDev)));
            sb.AppendLine(Loc.T("Info.Stars", d.Stars));
            sb.AppendLine(Diagnostics);
            sb.AppendLine();
            sb.AppendLine(Loc.T("Info.ChannelSummary"));
            sb.AppendLine($"R: {HfrR} · {StarsR} ★");
            sb.AppendLine($"G: {HfrG} · {StarsG} ★");
            sb.AppendLine($"B: {HfrB} · {StarsB} ★");
            sb.Append($"RGB: {HfrL} · {StarsL} ★");
            return sb.ToString();
        }

        private string status = Loc.T("Status.Waiting");
        public string Status { get => status; private set { status = value; RaisePropertyChanged(); } }

        /// <summary>
        /// NINA lanza ImagePrepared cada vez que prepara una imagen para mostrarla, se guarde o no
        /// (capturas manuales, imágenes abiertas desde disco, light frames de una secuencia...).
        /// Debe devolver enseguida; el análisis (varios segundos) corre en segundo plano.
        /// </summary>
        private void ImagingMediator_ImagePrepared(object sender, ImagePreparedEventArgs e) {
            try {
                var image = e.RenderedImage?.RawImageData;
                if (image == null) return;

                // NINA pide no detectar estrellas (DetectStars == false) en dos casos: la resolución de placas (capturas
                // de tipo SNAPSHOT) y cuando se pincha una imagen del histórico para verla. Lo primero se ignora;
                // lo segundo sí se analiza, para que el panel muestre la imagen elegida.
                if (e.Parameters?.DetectStars == false &&
                    string.Equals(image.MetaData?.Image?.ImageType, "SNAPSHOT", StringComparison.OrdinalIgnoreCase)) return;

                // NINA vuelve a preparar la misma imagen al cambiar el stretch u otras opciones: no se repite el análisis
                if (lastAnalyzed != null && lastAnalyzed.TryGetTarget(out var previous) && ReferenceEquals(previous, image)) return;

                if (!image.Properties.IsBayered) {
                    RunOnUi(() => Status = Loc.T("Status.NotColor"));
                    return;
                }

                // Si aún se está analizando la imagen anterior, se salta esta para no acumular trabajo
                if (System.Threading.Interlocked.Exchange(ref analyzing, 1) == 1) return;

                lastAnalyzed = new WeakReference<IImageData>(image);
                _ = Task.Run(() => AnalyzeImageAsync(image));
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: error starting the analysis: {ex}");
            }
        }

        private async Task AnalyzeImageAsync(IImageData image) {
            try {
                RunOnUi(() => Status = Loc.T("Status.Analyzing"));
                var result = await analyzer.AnalyzeAsync(image, System.Threading.CancellationToken.None);
                string captured = FormatCaptureTime(image);

                RunOnUi(() => {
                    // Primero los textos de los botones y la fecha: el cuadro emergente los usa al construirse
                    captureStatus = captured;
                    HfrR = Fmt(result.HfrR);
                    HfrG = Fmt(result.HfrG);
                    HfrB = Fmt(result.HfrB);
                    HfrL = Fmt(result.HfrL);
                    StarsR = result.StarsR.ToString(CultureInfo.InvariantCulture);
                    StarsG = result.StarsG.ToString(CultureInfo.InvariantCulture);
                    StarsB = result.StarsB.ToString(CultureInfo.InvariantCulture);
                    StarsL = result.StarsL.ToString(CultureInfo.InvariantCulture);
                    SetDetails(result);

                    if (result.StarsDetected < 5) {
                        Status = Loc.T("Status.FewStars", result.StarsDetected);
                        return;
                    }
                    Status = captured;
                    _ = EnsureMixAsync(); // si hay una mezcla seleccionada, se calcula también para esta imagen
                });
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: error analysing the image: {ex}");
                RunOnUi(() => Status = Loc.T("Status.Error"));
            } finally {
                System.Threading.Interlocked.Exchange(ref analyzing, 0);
            }
        }

        private static string Fmt(double v) =>
            double.IsNaN(v) ? "—" : v.ToString("F2", CultureInfo.InvariantCulture);

        private static void RunOnUi(Action action) {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }
    }
}

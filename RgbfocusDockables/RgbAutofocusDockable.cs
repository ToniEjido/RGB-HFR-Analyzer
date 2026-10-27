using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Model.Equipment;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Image.ImageAnalysis;
using NINA.Plugin.Interfaces;
using NINA.Profile;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Utility.AutoFocus;
using NINA.WPF.Base.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusDockables {

    /// <summary>Mejor posición del enfocador encontrada para un canal.</summary>
    public class FocusRecord {
        public string Channel { get; set; }
        public string Position { get; set; }
        public string Hfr { get; set; }
        public string Time { get; set; }
        /// <summary>Observación del ajuste (por ejemplo "fuera del rango medido").</summary>
        public string Note { get; set; }
        /// <summary>Valor numérico de la posición, para el botón "Ir".</summary>
        public int PositionValue { get; set; }
        public System.Windows.Input.ICommand GoCommand { get; set; }
    }

    /// <summary>
    /// Panel de autoenfoque por canal. Funciona como el de NINA (mismo motor de autoenfoque, misma gráfica),
    /// pero añade un desplegable con el canal del que se toma el HFR. Mientras dura el autoenfoque selecciona
    /// por sí solo el detector "RGB HFR Analyzer (active channel)" y después devuelve el detector que había.
    /// </summary>
    [Export(typeof(IDockableVM))]
    public class RgbAutofocusDockable : DockableVM {
        private readonly IProfileService appProfileService;
        private readonly ICameraMediator cameraMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;
        private readonly IPluggableBehaviorSelector<IStarDetection> detectorSelector;
        private CancellationTokenSource cancelSource;
        private IPluginOptionsAccessor pluginSettings;

        private const string BuiltInDetectorId = "NINA.Image.ImageAnalysis.StarDetection";
        private static readonly string OurDetectorId = typeof(RgbChannelStarDetection).FullName;
        private string previousDetectorId = BuiltInDetectorId;

        [ImportingConstructor]
        public RgbAutofocusDockable(
            IProfileService profileService,
            ICameraMediator cameraMediator,
            IFocuserMediator focuserMediator,
            IFilterWheelMediator filterWheelMediator,
            IAutoFocusVMFactory autoFocusVMFactory,
            IPluggableBehaviorSelector<IStarDetection> detectorSelector,
            IOptionsVM optionsVM) : base(profileService) {
            this.appProfileService = profileService;
            this.cameraMediator = cameraMediator;
            this.focuserMediator = focuserMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.detectorSelector = detectorSelector;

            Title = Loc.T("AF.Title");
            ImageGeometry = IconFactory.Create("AF", "RGB");

            try {
                var guid = Guid.Parse(((GuidAttribute)typeof(RgbAutofocusDockable).Assembly.GetCustomAttributes(typeof(GuidAttribute), false).First()).Value);
                pluginSettings = new PluginOptionsAccessor(profileService, guid);
                FocusFilePattern.Init(optionsVM, pluginSettings);
                string saved = pluginSettings.GetValueString("AutofocusChannel", "G");
                if (FocusChannelState.IsValid(saved)) {
                    selectedChannel = FocusChannelState.DisplayName(saved);
                    FocusChannelState.TrySelect(saved);
                }
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: could not read the saved channel: {ex.Message}");
            }

            this.detectorSelector.SelectedBehaviorChanged += (s, e) => {
                RaisePropertyChanged(nameof(UseForAllAutofocus));
                RaisePropertyChanged(nameof(GlobalHint));
            };

            // La selección mostrada sigue siempre al canal que mide el detector (también cuando un autoenfoque del secuenciador
            // lo cambia) y no se puede editar mientras hay un autoenfoque en curso
            FocusChannelState.ActiveChannelChanged += (s, e) => RunOnUi(SyncSelectedChannel);
            FocusChannelState.RunStateChanged += (s, e) => RunOnUi(() => {
                RaisePropertyChanged(nameof(IsChannelEditable));
                RaisePropertyChanged(nameof(UseForAllAutofocus));
                SyncSelectedChannel();
            });

            Records = new ObservableCollection<FocusRecord>();
            StartCommand = new ActionCommand(async _ => await StartAsync());
            CancelCommand = new ActionCommand(_ => cancelSource?.Cancel());

            TryCreateAutoFocusVM();
            Logger.Info("RGB HFR Analyzer: autofocus panel created");
        }

        /// <summary>Como el autoenfoque de NINA, aparece en el menú "Herramientas" y no en "Info".</summary>
        public override bool IsTool => true;

        public string[] Channels { get; } = { "R", "G", "B", "RGB", "R+G", "G+B", "R+B" };

        private static void RunOnUi(Action action) {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess()) action();
            else dispatcher.BeginInvoke(action);
        }

        /// <summary>El canal se puede cambiar solo si no hay ningún autoenfoque en curso (ni de este panel ni del secuenciador).</summary>
        public bool IsChannelEditable => !isRunning && !FocusChannelState.IsRunActive;

        /// <summary>Pone en pantalla el canal que mide el detector, sin guardarlo como preferencia del usuario.</summary>
        private void SyncSelectedChannel() {
            string display = FocusChannelState.DisplayName(FocusChannelState.ActiveChannel);
            if (selectedChannel != display) selectedChannel = display;
            RaisePropertyChanged(nameof(SelectedChannel));
            RaisePropertyChanged(nameof(GlobalHint));
        }

        private string selectedChannel = "G";
        public string SelectedChannel {
            get => selectedChannel;
            set {
                string key = FocusChannelState.FromDisplayName(value);
                // Con un autoenfoque en marcha el canal no cambia: se vuelve a mostrar el que se está midiendo
                if (!FocusChannelState.TrySelect(key)) {
                    RaisePropertyChanged();
                    return;
                }
                selectedChannel = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GlobalHint));
                try { pluginSettings?.SetValueString("AutofocusChannel", key); } catch { }
            }
        }

        /// <summary>
        /// Si está activado, el detector del plugin queda seleccionado de forma permanente en NINA, de modo que
        /// TODOS los autoenfoques (manual, secuenciador heredado y avanzado, disparadores) miden el canal elegido.
        /// Al desactivarlo se devuelve el detector que había antes.
        /// </summary>
        public bool UseForAllAutofocus {
            get => detectorSelector.SelectedBehavior?.ContentId == OurDetectorId;
            set {
                // Durante un autoenfoque el detector queda fijo
                if (FocusChannelState.IsRunActive) {
                    RaisePropertyChanged();
                    return;
                }
                if (value) {
                    var current = detectorSelector.SelectedBehavior;
                    if (current != null && current.ContentId != OurDetectorId) previousDetectorId = current.ContentId;
                    var ours = detectorSelector.Behaviors.FirstOrDefault(b => b.ContentId == OurDetectorId);
                    if (ours != null) detectorSelector.SelectedBehavior = ours;
                    else Message = Loc.T("AF.NoDetector");
                } else {
                    var previous = detectorSelector.Behaviors.FirstOrDefault(b => b.ContentId == previousDetectorId)
                                   ?? detectorSelector.Behaviors.FirstOrDefault(b => b.ContentId != OurDetectorId);
                    if (previous != null) detectorSelector.SelectedBehavior = previous;
                }
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(GlobalHint));
            }
        }

        public string GlobalHint => UseForAllAutofocus
            ? Loc.T("AF.GlobalHint", selectedChannel)
            : string.Empty;

        private IAutoFocusVM autoFocusVM;
        /// <summary>Instancia del motor de autoenfoque de NINA; NINA dibuja su gráfica con su propia plantilla.</summary>
        public IAutoFocusVM AutoFocusVM { get => autoFocusVM; private set { autoFocusVM = value; RaisePropertyChanged(); } }

        private bool isRunning;
        public bool IsRunning {
            get => isRunning;
            private set {
                isRunning = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsIdle));
                RaisePropertyChanged(nameof(IsChannelEditable));
                RaisePropertyChanged(nameof(StartVisibility));
                RaisePropertyChanged(nameof(CancelVisibility));
            }
        }
        public bool IsIdle => !isRunning;
        public Visibility StartVisibility => isRunning ? Visibility.Collapsed : Visibility.Visible;
        public Visibility CancelVisibility => isRunning ? Visibility.Visible : Visibility.Collapsed;

        private string status = string.Empty;
        public string Status { get => status; private set { status = value; RaisePropertyChanged(); } }

        private string message = string.Empty;
        public string Message { get => message; private set { message = value; RaisePropertyChanged(); } }

        public ObservableCollection<FocusRecord> Records { get; }


        public ICommand StartCommand { get; }
        public ICommand CancelCommand { get; }

        private void TryCreateAutoFocusVM() {
            try {
                AutoFocusVM = autoFocusVMFactory.Create();
            } catch (Exception ex) {
                Logger.Debug($"RGB HFR Analyzer: the autofocus engine is not ready yet: {ex.Message}");
            }
        }

        private FilterInfo GetCurrentFilter() {
            var info = filterWheelMediator.GetInfo();
            if (info?.SelectedFilter == null) return null;
            return appProfileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters
                .FirstOrDefault(x => x.Position == info.SelectedFilter.Position);
        }

        private async Task StartAsync() {
            if (isRunning) return;

            if (cameraMediator.GetInfo()?.Connected != true) { Message = Loc.T("AF.NoCamera"); return; }
            if (focuserMediator.GetInfo()?.Connected != true) { Message = Loc.T("AF.NoFocuser"); return; }
            if (!cameraMediator.IsFreeToCapture(this)) { Message = Loc.T("AF.CameraBusy"); return; }

            Message = string.Empty;
            Status = string.Empty;
            cancelSource?.Dispose();
            cancelSource = new CancellationTokenSource();
            IsRunning = true;

            cameraMediator.RegisterCaptureBlock(this);
            try {
                string channel = FocusChannelState.FromDisplayName(selectedChannel);

                // Falla con un mensaje claro si el canal no se puede medir (cámara mono, binning...)
                ChannelPreflight.EnsureForAutofocus(channel, cameraMediator, filterWheelMediator, appProfileService);

                // Mientras dura: canal y detector fijos, aunque se toquen desde la interfaz o desde el secuenciador
                using (ChannelRunScope.Begin(channel, detectorSelector)) {
                    var vm = autoFocusVMFactory.Create();
                    AutoFocusVM = vm;

                    var report = await vm.StartAutoFocus(GetCurrentFilter(), cancelSource.Token, new Progress<ApplicationStatus>(p => Status = p?.Status ?? string.Empty));
                    if (report?.CalculatedFocusPoint != null) {
                        SaveRecord(report.CalculatedFocusPoint, channel);
                        FocusFilePattern.Update(channel);
                    }
                }
            } catch (OperationCanceledException) {
                Message = Loc.T("AF.Cancelled");
            } catch (ChannelNotMeasurableException ex) {
                Logger.Error($"RGB HFR Analyzer: {ex.Message}");
                Message = ex.Message;
            } catch (ChannelRunRefusedException ex) {
                Message = ex.Message;
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: autofocus error: {ex}");
                Message = Loc.T("AF.Error");
            } finally {
                cameraMediator.ReleaseCaptureBlock(this);
                IsRunning = false;
            }
        }

        /// <summary>Mueve el enfocador a una de las posiciones de la tabla.</summary>
        private async Task GoToAsync(int position) {
            if (isRunning) return;
            if (focuserMediator.GetInfo()?.Connected != true) { Message = Loc.T("AF.NoFocuser"); return; }
            if (!cameraMediator.IsFreeToCapture(this)) { Message = Loc.T("AF.CameraBusy"); return; }
            try {
                Message = string.Empty;
                Status = Loc.T("AF.Moving", position);
                await focuserMediator.MoveFocuser(position, CancellationToken.None);
                Status = Loc.T("AF.Moved", position);
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: error moving the focuser: {ex}");
                Message = Loc.T("AF.MoveError");
            }
        }

        private void SaveRecord(FocusPoint point, string channel) {
            var record = new FocusRecord {
                Channel = FocusChannelState.DisplayName(channel),
                Position = point.Position.ToString("0", CultureInfo.InvariantCulture),
                Hfr = point.Value.ToString("0.00", CultureInfo.InvariantCulture),
                Time = DateTime.Now.ToString("G", appProfileService.ActiveProfile.ApplicationSettings.Language ?? CultureInfo.CurrentCulture)
            };

            record.PositionValue = (int)point.Position;
            record.GoCommand = new ActionCommand(async _ => await GoToAsync(record.PositionValue));
            var existing = Records.FirstOrDefault(r => r.Channel == record.Channel);
            if (existing != null) Records.Remove(existing);
            Records.Add(record);
        }
    }
}

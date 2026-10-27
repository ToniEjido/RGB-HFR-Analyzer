using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using Newtonsoft.Json;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Validations;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusTestCategory {

    /// <summary>
    /// Instrucción del secuenciador avanzado: autoenfoque de NINA midiendo el HFR de un canal (R, G, B o RGB).
    /// Por dentro ejecuta la propia instrucción "Ejecutar autoenfoque" de NINA, así que se comporta igual
    /// (ventana de autoenfoque, historial, filtro actual...).
    /// </summary>
    [ExportMetadata("Name", "Channel Autofocus")]
    [ExportMetadata("Description", "Runs NINA's autofocus taking the HFR of the chosen channel (R, G, B, RGB or a mix of two)")]
    [ExportMetadata("Icon", "AutoFocusSVG")]
    [ExportMetadata("Category", "RGB HFR Analyzer")]
    [Export(typeof(ISequenceItem))]
    [JsonObject(MemberSerialization.OptIn)]
    public class RgbAutofocusChannelInstruction : SequenceItem, IValidatable {
        private readonly IProfileService profileService;
        private readonly IImageHistoryVM history;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;
        private readonly IPluggableBehaviorSelector<IStarDetection> detectorSelector;

        [ImportingConstructor]
        public RgbAutofocusChannelInstruction(
            IProfileService profileService,
            IImageHistoryVM history,
            ICameraMediator cameraMediator,
            IFilterWheelMediator filterWheelMediator,
            IFocuserMediator focuserMediator,
            IAutoFocusVMFactory autoFocusVMFactory,
            IPluggableBehaviorSelector<IStarDetection> detectorSelector) {
            this.profileService = profileService;
            this.history = history;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.detectorSelector = detectorSelector;
        }

        private RgbAutofocusChannelInstruction(RgbAutofocusChannelInstruction cloneMe)
            : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory, cloneMe.detectorSelector) {
            CopyMetaData(cloneMe);
            Channel = cloneMe.Channel;
        }

        private string channel = "G";

        /// <summary>Clave del canal: "R", "G", "B", "L" (RGB), "R+G", "G+B" o "R+B".</summary>
        [JsonProperty]
        public string Channel {
            get => channel;
            set {
                channel = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ChannelDisplay));
            }
        }

        public string[] Channels { get; } = { "R", "G", "B", "RGB", "R+G", "G+B", "R+B" };

        public string ChannelDisplay {
            get => FocusChannelState.DisplayName(channel);
            set => Channel = FocusChannelState.FromDisplayName(value);
        }

        private RunAutofocus CreateAutofocus() =>
            new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory);

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await ChannelAutofocus.RunAsync(channel, detectorSelector, cameraMediator, filterWheelMediator, profileService, () => CreateAutofocus().Execute(progress, token));
        }

        private IList<string> issues = new List<string>();

        public IList<string> Issues {
            get => issues;
            set {
                issues = value;
                RaisePropertyChanged();
            }
        }

        public bool Validate() {
            var i = new List<string>();
            if (!cameraMediator.GetInfo().Connected) i.Add(Localization.Loc.T("Seq.NoCamera"));
            if (!focuserMediator.GetInfo().Connected) i.Add(Localization.Loc.T("Seq.NoFocuser"));
            if (cameraMediator.GetInfo().Connected) {
                // El canal elegido tiene que poder medirse con esta cámara y este binning de autoenfoque
                var problem = ChannelPreflight.Check(channel, cameraMediator, filterWheelMediator, profileService, out int bx, out int by);
                if (problem != MeasurabilityProblem.None) i.Add(ChannelPreflight.Describe(problem, channel, bx, by));
            }
            Issues = i;
            return i.Count == 0;
        }

        public override TimeSpan GetEstimatedDuration() => CreateAutofocus().GetEstimatedDuration();

        public override object Clone() => new RgbAutofocusChannelInstruction(this);

        public override string ToString() =>
            $"Category: {Category}, Item: {nameof(RgbAutofocusChannelInstruction)}, Channel: {channel}";
    }
}

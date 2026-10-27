using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusDockables;
using Newtonsoft.Json;
using NINA.Core.Interfaces;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using NINA.Sequencer.Container;
using NINA.Sequencer.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.SequenceItem.Autofocus;
using NINA.Sequencer.Utility;
using NINA.Sequencer.Trigger;
using NINA.WPF.Base.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusTestCategory {

    /// <summary>Un tramo del plan: canal a enfocar y número de exposiciones LIGHT que dura.</summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ChannelPlanStep : BaseINPC {
        private string channel = "R";
        private int exposures = 20;

        public string[] Channels { get; } = { "R", "G", "B", "RGB", "R+G", "G+B", "R+B" };

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

        [JsonProperty]
        public int Exposures {
            get => exposures;
            set {
                exposures = Math.Max(1, value);
                RaisePropertyChanged();
            }
        }

        public string ChannelDisplay {
            get => FocusChannelState.DisplayName(channel);
            set => Channel = FocusChannelState.FromDisplayName(value);
        }

        public ChannelPlanStep Copy() => new ChannelPlanStep { Channel = Channel, Exposures = Exposures };
    }

    /// <summary>
    /// Disparador del secuenciador avanzado que sigue un plan de canales. Cada tramo indica un canal y cuántas exposiciones
    /// LIGHT dura; al terminar un tramo pasa al siguiente canal y ejecuta el autoenfoque de NINA midiendo ese canal.
    /// Se pueden programar varios cambios y, opcionalmente, repetir el plan.
    /// </summary>
    [ExportMetadata("Name", "Channel Change and Refocus")]
    [ExportMetadata("Description", "Follows a channel plan: after the number of exposures set in each step, switches channel and runs autofocus on it")]
    [ExportMetadata("Icon", "AutoFocusAfterExposuresSVG")]
    [ExportMetadata("Category", "RGB HFR Analyzer")]
    [Export(typeof(ISequenceTrigger))]
    [JsonObject(MemberSerialization.OptIn)]
    public class RgbChannelPlanTrigger : SequenceTrigger {
        private readonly IProfileService profileService;
        private readonly IImageHistoryVM history;
        private readonly ICameraMediator cameraMediator;
        private readonly IFilterWheelMediator filterWheelMediator;
        private readonly IFocuserMediator focuserMediator;
        private readonly IAutoFocusVMFactory autoFocusVMFactory;
        private readonly IPluggableBehaviorSelector<IStarDetection> detectorSelector;

        // Estado de ejecución (no se guarda en el archivo de secuencia)
        private readonly ChannelPlanTracker plan = new ChannelPlanTracker();

        [ImportingConstructor]
        public RgbChannelPlanTrigger(
            IProfileService profileService,
            IImageHistoryVM history,
            ICameraMediator cameraMediator,
            IFilterWheelMediator filterWheelMediator,
            IFocuserMediator focuserMediator,
            IAutoFocusVMFactory autoFocusVMFactory,
            IPluggableBehaviorSelector<IStarDetection> detectorSelector) : base() {
            this.profileService = profileService;
            this.history = history;
            this.cameraMediator = cameraMediator;
            this.filterWheelMediator = filterWheelMediator;
            this.focuserMediator = focuserMediator;
            this.autoFocusVMFactory = autoFocusVMFactory;
            this.detectorSelector = detectorSelector;

            Steps = new ObservableCollection<ChannelPlanStep> {
                new ChannelPlanStep { Channel = "R", Exposures = 20 },
                new ChannelPlanStep { Channel = "B", Exposures = 20 }
            };
            AddStepCommand = new ActionCommand(_ => Steps.Add(new ChannelPlanStep { Channel = "G", Exposures = 20 }));
            RemoveStepCommand = new ActionCommand(step => {
                if (step is ChannelPlanStep s && Steps.Count > 1) Steps.Remove(s);
            });
        }

        private RgbChannelPlanTrigger(RgbChannelPlanTrigger cloneMe)
            : this(cloneMe.profileService, cloneMe.history, cloneMe.cameraMediator, cloneMe.filterWheelMediator, cloneMe.focuserMediator, cloneMe.autoFocusVMFactory, cloneMe.detectorSelector) {
            CopyMetaData(cloneMe);
            Steps = new ObservableCollection<ChannelPlanStep>(cloneMe.Steps.Select(s => s.Copy()));
            Repeat = cloneMe.Repeat;
            FocusAtStart = cloneMe.FocusAtStart;
        }

        /// <summary>Tramos del plan. Se reemplaza al cargar para no duplicar los tramos por defecto.</summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<ChannelPlanStep> Steps { get; set; }

        /// <summary>Al terminar el último tramo, volver al primero.</summary>
        [JsonProperty]
        public bool Repeat { get; set; } = true;

        /// <summary>Enfocar el primer canal del plan antes de la primera exposición.</summary>
        [JsonProperty]
        public bool FocusAtStart { get; set; } = true;

        public ICommand AddStepCommand { get; }
        public ICommand RemoveStepCommand { get; }

        private string progressText = string.Empty;
        public string ProgressText {
            get => progressText;
            private set {
                progressText = value;
                RaisePropertyChanged();
            }
        }

        private int LightCount() => history.ImageHistory.Count(x => x.Type == "LIGHT");

        private RunAutofocus CreateAutofocus() =>
            new RunAutofocus(profileService, history, cameraMediator, filterWheelMediator, focuserMediator, autoFocusVMFactory);

        public override bool ShouldTrigger(ISequenceItem previousItem, ISequenceItem nextItem) {
            if (!(nextItem is IExposureItem exposure) || exposure.ImageType != "LIGHT") return false;
            if (Steps == null || Steps.Count == 0) return false;

            var evaluation = plan.Evaluate(
                LightCount(),
                Steps.Select(s => s.Exposures).ToList(),
                Repeat,
                FocusAtStart,
                () => IsTooCloseToMeridianFlip(nextItem));

            if (evaluation.StartedWithoutFocus) {
                // El plan empieza sin autoenfoque inicial: el primer canal ya está activo
                FocusChannelState.TrySelect(Steps[0].Channel);
            }

            if (evaluation.Step >= 0 && evaluation.Step < Steps.Count) {
                var step = Steps[evaluation.Step];
                ProgressText = $"{step.ChannelDisplay}: {evaluation.Taken}/{step.Exposures}";
            }

            switch (evaluation.Decision) {
                case PlanDecision.Done:
                    ProgressText = Localization.Loc.T("Seq.PlanDone");
                    return false;
                case PlanDecision.PostponedMeridianFlip:
                    Logger.Warning("RGB HFR Analyzer: the channel change and refocus should run, but the meridian flip is too close to finish it first");
                    return false;
                case PlanDecision.Focus:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Igual que los disparadores de autoenfoque de NINA: si hay un volteo meridiano programado y el autoenfoque (más la exposición
        /// siguiente) no termina antes, no se dispara; se vuelve a comprobar antes de la siguiente exposición.
        /// </summary>
        private bool IsTooCloseToMeridianFlip(ISequenceItem nextItem) {
            var duration = CreateAutofocus().GetEstimatedDuration() + (nextItem?.GetEstimatedDuration() ?? TimeSpan.Zero);
            return ItemUtility.IsTooCloseToMeridianFlip(Parent, duration);
        }

        public override async Task Execute(ISequenceContainer context, IProgress<ApplicationStatus> progress, CancellationToken token) {
            int next = plan.NextStep(Steps.Count);
            var step = Steps[next];

            Logger.Info($"RGB HFR Analyzer: switching to channel {step.ChannelDisplay} and refocusing (step {next + 1} of {Steps.Count})");
            await ChannelAutofocus.RunAsync(step.Channel, detectorSelector, cameraMediator, filterWheelMediator, profileService, () => CreateAutofocus().Execute(progress, token));

            // El estado solo avanza si el autoenfoque terminó bien
            plan.Advance(next, LightCount());
            ProgressText = $"{step.ChannelDisplay}: 0/{step.Exposures}";
        }

        public override object Clone() => new RgbChannelPlanTrigger(this);

        public override string ToString() =>
            $"Category: {Category}, Trigger: {nameof(RgbChannelPlanTrigger)}, Steps: {string.Join(", ", Steps.Select(s => s.ChannelDisplay + "x" + s.Exposures))}";
    }
}

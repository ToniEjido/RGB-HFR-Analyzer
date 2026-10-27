using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using System;
using System.Linq;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>No se puede empezar el autoenfoque por canal (no hay detector del plugin, o ya hay otro en marcha).</summary>
    public sealed class ChannelRunRefusedException : InvalidOperationException {
        public ChannelRunRefusedException(string message) : base(message) { }
    }

    /// <summary>
    /// Todo lo que protege a un autoenfoque por canal mientras dura: bloquea el canal (FocusChannelState.BeginRun), selecciona el
    /// detector del plugin y lo mantiene seleccionado aunque alguien lo cambie, y al terminar devuelve el detector anterior y
    /// libera el canal. Lo usan igual el panel de autoenfoque y las instrucciones del secuenciador.
    /// </summary>
    public sealed class ChannelRunScope : IDisposable {
        private readonly IPluggableBehaviorSelector<IStarDetection> selector;
        private readonly IStarDetection ours;
        private readonly IStarDetection previous;
        private readonly IDisposable channelLock;
        private bool reasserting;
        private bool disposed;

        private ChannelRunScope(IPluggableBehaviorSelector<IStarDetection> selector, IStarDetection ours, IDisposable channelLock) {
            this.selector = selector;
            this.ours = ours;
            this.channelLock = channelLock;
            previous = selector.SelectedBehavior;
        }

        /// <summary>
        /// Empieza la protección. Lanza ChannelRunRefusedException si ya hay otro autoenfoque por canal en marcha, o
        /// ChannelRunRefusedException si no se encuentra el detector del plugin.
        /// </summary>
        public static ChannelRunScope Begin(string channel, IPluggableBehaviorSelector<IStarDetection> selector) {
            var ours = selector.Behaviors.FirstOrDefault(b => b.ContentId == typeof(RgbChannelStarDetection).FullName);
            if (ours == null) throw new ChannelRunRefusedException(Loc.T("AF.NoDetector"));

            IDisposable channelLock;
            try {
                channelLock = FocusChannelState.BeginRun(channel);
            } catch (FocusRunInProgressException) {
                throw new ChannelRunRefusedException(Loc.T("AF.RunInProgress"));
            }

            var scope = new ChannelRunScope(selector, ours, channelLock);
            try {
                selector.SelectedBehavior = ours;
                selector.SelectedBehaviorChanged += scope.OnDetectorChanged;
            } catch {
                scope.Dispose();
                throw;
            }
            return scope;
        }

        // Si durante el autoenfoque alguien cambia el detector (por ejemplo desde las opciones de NINA), se vuelve a poner el
        // del plugin: con otro detector la curva se mediría sobre la imagen completa, no sobre el canal.
        private void OnDetectorChanged(object sender, EventArgs e) {
            if (disposed || reasserting) return;
            if (selector.SelectedBehavior?.ContentId == ours.ContentId) return;
            try {
                reasserting = true;
                Logger.Warning("RGB HFR Analyzer: the star detector was changed during a channel autofocus; restoring it");
                selector.SelectedBehavior = ours;
            } finally {
                reasserting = false;
            }
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            try {
                selector.SelectedBehaviorChanged -= OnDetectorChanged;
                // Se devuelve siempre el detector que había antes
                if (previous != null) selector.SelectedBehavior = previous;
            } finally {
                channelLock.Dispose();
            }
        }
    }
}

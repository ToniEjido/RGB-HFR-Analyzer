using NINA.Core.Interfaces;
using NINA.Image.ImageAnalysis;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Ejecuta un autoenfoque de NINA midiendo un canal concreto: fija el canal activo, selecciona el detector del plugin
    /// mientras dura y después devuelve el detector que había antes. Lo usan las instrucciones del secuenciador.
    /// </summary>
    public static class ChannelAutofocus {
        public static async Task RunAsync(string channel, IPluggableBehaviorSelector<IStarDetection> selector, Func<Task> runAutofocus) {
            var ours = selector.Behaviors.FirstOrDefault(b => b.ContentId == typeof(RgbChannelStarDetection).FullName);
            if (ours == null) {
                throw new InvalidOperationException(Localization.Loc.T("AF.NoDetector"));
            }

            var previous = selector.SelectedBehavior;
            FocusChannelState.ActiveChannel = channel;
            try {
                selector.SelectedBehavior = ours;
                await runAutofocus();
                FocusFilePattern.Update(channel);
            } finally {
                if (previous != null) selector.SelectedBehavior = previous;
            }
        }
    }
}

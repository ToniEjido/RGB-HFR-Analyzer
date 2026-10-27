using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageAnalysis;
using NINA.Profile.Interfaces;
using System;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Ejecuta un autoenfoque de NINA midiendo un canal concreto. Antes de empezar comprueba que ese canal se puede medir
    /// (si no, falla con un mensaje claro), bloquea el canal y el detector mientras dura (ChannelRunScope) y solo anota el canal
    /// en el patrón de nombre de archivo si el autoenfoque terminó bien. Lo usan las instrucciones del secuenciador.
    /// </summary>
    public static class ChannelAutofocus {
        public static async Task RunAsync(
            string channel,
            IPluggableBehaviorSelector<IStarDetection> selector,
            ICameraMediator camera,
            IFilterWheelMediator filterWheel,
            IProfileService profile,
            Func<Task> runAutofocus) {

            ChannelPreflight.EnsureForAutofocus(channel, camera, filterWheel, profile);

            using (ChannelRunScope.Begin(channel, selector)) {
                await runAutofocus();
                FocusFilePattern.Update(channel);
            }
        }
    }
}

using System;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    public enum MeasurabilityProblem {
        None,
        /// <summary>La clave de canal no existe.</summary>
        InvalidChannel,
        /// <summary>La cámara o la imagen no es de color (Bayer): no hay canales R, G y B separados.</summary>
        NotColor,
        /// <summary>La imagen tiene binning: el binning mezcla los píxeles vecinos y destruye el patrón Bayer.</summary>
        Binned
    }

    /// <summary>
    /// Decide si un canal se puede medir de verdad. Un canal (R, G, B o una mezcla) solo existe en una imagen de color sin binning;
    /// "RGB" (L) es la detección normal de NINA sobre la imagen completa y se puede medir siempre.
    /// Es lógica pura, sin NINA, para poder probarla.
    /// </summary>
    public static class ChannelMeasurability {
        public static MeasurabilityProblem Check(string channel, bool isColor, int binX, int binY) {
            if (!FocusChannelState.IsValid(channel)) return MeasurabilityProblem.InvalidChannel;
            if (channel == "L") return MeasurabilityProblem.None;
            if (!isColor) return MeasurabilityProblem.NotColor;
            if (binX > 1 || binY > 1) return MeasurabilityProblem.Binned;
            return MeasurabilityProblem.None;
        }
    }

    /// <summary>El canal pedido no se puede medir con la cámara o la imagen actual. El mensaje explica el motivo y qué hacer.</summary>
    public sealed class ChannelNotMeasurableException : InvalidOperationException {
        public MeasurabilityProblem Problem { get; }
        public string Channel { get; }

        public ChannelNotMeasurableException(MeasurabilityProblem problem, string channel, string message) : base(message) {
            Problem = problem;
            Channel = channel;
        }
    }
}

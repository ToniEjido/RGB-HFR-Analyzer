using System;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Canal que debe medir el detector de estrellas del plugin: "R", "G", "B" o "L" (RGB, luminancia).
    /// Es estático a propósito: lo comparten el panel de autoenfoque, el detector y, más adelante, las
    /// instrucciones del secuenciador, sin depender de cómo NINA cree cada objeto.
    /// </summary>
    public static class FocusChannelState {
        private static string active = "G";

        public static string ActiveChannel {
            get => active;
            set {
                if (!IsValid(value)) return;
                if (active == value) return;
                active = value;
                ActiveChannelChanged?.Invoke(null, EventArgs.Empty);
            }
        }

        public static event EventHandler ActiveChannelChanged;

        /// <summary>Claves válidas: canales simples, luminancia ("L") y mezclas de dos canales ("R+G", "G+B", "R+B").</summary>
        public static bool IsValid(string key) =>
            key == "R" || key == "G" || key == "B" || key == "L" || key == "R+G" || key == "G+B" || key == "R+B";

        /// <summary>Texto para nombres de archivo: sin "+" (R+B -> RB) y "L" como RGB.</summary>
        public static string FileTag(string key) => key == "L" ? "RGB" : (key ?? string.Empty).Replace("+", string.Empty);

        public static string DisplayName(string key) => key == "L" ? "RGB" : key;
        public static string FromDisplayName(string name) => name == "RGB" ? "L" : name;
    }
}

using System;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>La selección de canal no puede cambiar porque hay un autoenfoque en curso.</summary>
    public sealed class FocusRunInProgressException : InvalidOperationException {
        public FocusRunInProgressException(string message) : base(message) { }
    }

    /// <summary>
    /// Canal que debe medir el detector de estrellas del plugin: "R", "G", "B" o "L" (RGB, luminancia), o una mezcla de dos.
    /// Es estático a propósito: lo comparten el panel de autoenfoque, el detector y las instrucciones del secuenciador,
    /// sin depender de cómo NINA cree cada objeto.
    ///
    /// Hay dos valores:
    ///  - SelectedChannel: el que ha elegido el usuario (panel de autoenfoque).
    ///  - ActiveChannel: el que mide el detector. Es el elegido, salvo mientras dura un autoenfoque (BeginRun): entonces es el canal
    ///    de esa ejecución y no cambia, ni desde la interfaz ni desde otro elemento del secuenciador, hasta que termina.
    /// Así una misma curva de enfoque nunca mezcla canales distintos.
    /// </summary>
    public static class FocusChannelState {
        private static readonly object gate = new object();
        private static string selected = "G";
        private static string running;

        /// <summary>Canal que mide el detector ahora mismo.</summary>
        public static string ActiveChannel {
            get { lock (gate) return running ?? selected; }
        }

        /// <summary>Canal elegido por el usuario (el que vuelve a ser el activo al terminar un autoenfoque).</summary>
        public static string SelectedChannel {
            get { lock (gate) return selected; }
        }

        /// <summary>True mientras hay un autoenfoque en curso con su canal bloqueado.</summary>
        public static bool IsRunActive {
            get { lock (gate) return running != null; }
        }

        /// <summary>Cambia el canal activo (por selección del usuario o al empezar/terminar un autoenfoque).</summary>
        public static event EventHandler ActiveChannelChanged;

        /// <summary>Empieza o termina un autoenfoque con canal bloqueado.</summary>
        public static event EventHandler RunStateChanged;

        /// <summary>
        /// Elige el canal. Devuelve false, sin cambiar nada, si la clave no es válida o si hay un autoenfoque en curso.
        /// </summary>
        public static bool TrySelect(string key) {
            bool changed;
            lock (gate) {
                if (!IsValid(key) || running != null) return false;
                changed = selected != key;
                selected = key;
            }
            if (changed) ActiveChannelChanged?.Invoke(null, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Bloquea el canal para un autoenfoque. Mientras no se libere el resultado (Dispose), el detector mide siempre este canal
        /// y TrySelect no lo cambia. Solo puede haber un autoenfoque a la vez: si ya hay otro, lanza FocusRunInProgressException.
        /// </summary>
        public static IDisposable BeginRun(string channel) {
            if (!IsValid(channel)) throw new ArgumentException("Invalid channel: " + channel, nameof(channel));
            bool changed;
            lock (gate) {
                if (running != null) throw new FocusRunInProgressException(running);
                changed = (running ?? selected) != channel;
                running = channel;
            }
            RunStateChanged?.Invoke(null, EventArgs.Empty);
            if (changed) ActiveChannelChanged?.Invoke(null, EventArgs.Empty);
            return new RunToken();
        }

        private static void EndRun() {
            bool changed;
            lock (gate) {
                if (running == null) return;
                changed = running != selected;
                running = null;
            }
            RunStateChanged?.Invoke(null, EventArgs.Empty);
            if (changed) ActiveChannelChanged?.Invoke(null, EventArgs.Empty);
        }

        private sealed class RunToken : IDisposable {
            private int disposed;
            public void Dispose() {
                if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 0) EndRun();
            }
        }

        /// <summary>Claves válidas: canales simples, luminancia ("L") y mezclas de dos canales ("R+G", "G+B", "R+B").</summary>
        public static bool IsValid(string key) =>
            key == "R" || key == "G" || key == "B" || key == "L" || key == "R+G" || key == "G+B" || key == "R+B";

        /// <summary>Texto para nombres de archivo: sin "+" (R+B -> RB) y "L" como RGB.</summary>
        public static string FileTag(string key) => key == "L" ? "RGB" : (key ?? string.Empty).Replace("+", string.Empty);

        public static string DisplayName(string key) => key == "L" ? "RGB" : key;
        public static string FromDisplayName(string name) => name == "RGB" ? "L" : name;
    }
}

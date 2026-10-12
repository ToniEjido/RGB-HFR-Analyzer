using AntonioAguadoRovira.NINA.Rgbfocus.Properties;
using System;
using System.Globalization;
using System.Threading;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Localization {

    /// <summary>
    /// Idioma de la interfaz del plugin. Por defecto sigue el idioma de NINA (español si es español, inglés en cualquier otro caso);
    /// en Opciones > Plugins se puede forzar uno de los dos. El cambio se aplica al reiniciar NINA.
    /// </summary>
    public static class Loc {
        private static bool? spanish;

        public static bool IsSpanish {
            get {
                if (spanish == null) spanish = Resolve();
                return spanish.Value;
            }
        }

        private static bool Resolve() {
            string setting = "auto";
            try { setting = (Settings.Default.Language ?? "auto").ToLowerInvariant(); } catch { }
            if (setting == "es") return true;
            if (setting == "en") return false;
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "es"
                || Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName == "es";
        }

        /// <summary>Texto de la clave en el idioma activo. Si falta, devuelve la versión en inglés o la propia clave.</summary>
        public static string T(string key) {
            var table = IsSpanish ? Strings.Es : Strings.En;
            if (table.TryGetValue(key, out var text)) return text;
            return Strings.En.TryGetValue(key, out text) ? text : key;
        }

        public static string T(string key, params object[] args) {
            try { return string.Format(CultureInfo.CurrentCulture, T(key), args); } catch (FormatException) { return T(key); }
        }
    }
}

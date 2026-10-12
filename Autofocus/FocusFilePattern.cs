using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.ViewModel;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>
    /// Registra el patrón de nombre de archivo $$FOCUSCHANNEL$$ (Opciones > Imagen > Patrón de nombre de archivo).
    /// Su valor es el canal con el que se hizo el último autoenfoque desde este plugin: R, G, B, RGB, RG, GB o RB.
    /// Se actualiza al terminar cada autoenfoque y se recuerda entre sesiones.
    /// </summary>
    public static class FocusFilePattern {
        public const string Key = "$$FOCUSCHANNEL$$";
        private const string SettingKey = "LastFocusedChannel";

        private static IOptionsVM optionsVM;
        private static IPluginOptionsAccessor settings;
        private static string current = string.Empty;

        public static void Init(IOptionsVM options, IPluginOptionsAccessor accessor) {
            optionsVM = options;
            settings = accessor;
            try { current = settings?.GetValueString(SettingKey, string.Empty) ?? string.Empty; } catch { current = string.Empty; }
            Register();
        }

        /// <summary>Guarda el canal del último autoenfoque y actualiza el valor del patrón.</summary>
        public static void Update(string channelKey) {
            current = FocusChannelState.FileTag(channelKey);
            try { settings?.SetValueString(SettingKey, current); } catch { }
            Register();
        }

        private static ImagePattern pattern;

        /// <summary>
        /// NINA 3.2 solo permite añadir patrones (no quitarlos), así que se registra una única instancia
        /// y después solo se cambia su valor.
        /// </summary>
        private static void Register() {
            if (optionsVM == null) return;
            try {
                if (pattern == null) {
                    pattern = new ImagePattern(Key, Loc.T("Pattern.Desc"), "RGB HFR Analyzer") { Value = current };
                    optionsVM.AddImagePattern(pattern);
                } else {
                    pattern.Value = current;
                }
            } catch (System.Exception ex) {
                Logger.Error($"RGB HFR Analyzer: could not register the pattern {Key}: {ex.Message}");
            }
        }
    }
}

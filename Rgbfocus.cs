using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using AntonioAguadoRovira.NINA.Rgbfocus.Properties;
using NINA.Core.Utility;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Settings = AntonioAguadoRovira.NINA.Rgbfocus.Properties.Settings;

namespace AntonioAguadoRovira.NINA.Rgbfocus {
    /// <summary>
    /// Manifiesto del plugin RGB HFR Analyzer. La lógica vive en RgbfocusDockable (panel de análisis del tab Imaging),
    /// RgbAutofocusDockable (autoenfoque por canal) y Analysis/RgbHfrAnalyzer.cs.
    /// </summary>
    [Export(typeof(IPluginManifest))]
    public class Rgbfocus : PluginBase, INotifyPropertyChanged {

        [ImportingConstructor]
        public Rgbfocus(IProfileService profileService) {
            if (Settings.Default.UpdateSettings) {
                Settings.Default.Upgrade();
                Settings.Default.UpdateSettings = false;
                CoreUtil.SaveSettings(Settings.Default);
            }
        }

        // ---- Opciones (Opciones > Plugins): idioma de la interfaz ----

        /// <summary>Opciones del desplegable de idioma: "Automático", "English", "Español".</summary>
        public IList<string> LanguageOptions => new List<string> { Loc.T("Opt.Auto"), "English", "Español" };

        public int LanguageIndex {
            get {
                switch ((Settings.Default.Language ?? "auto").ToLowerInvariant()) {
                    case "en": return 1;
                    case "es": return 2;
                    default: return 0;
                }
            }
            set {
                Settings.Default.Language = value == 1 ? "en" : value == 2 ? "es" : "auto";
                CoreUtil.SaveSettings(Settings.Default);
                RaisePropertyChanged();
            }
        }

        public string LanguageLabel => Loc.T("Opt.Language");
        public string RestartNote => Loc.T("Opt.Restart");

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

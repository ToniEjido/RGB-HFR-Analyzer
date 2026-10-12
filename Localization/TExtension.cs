using System;
using System.Windows.Markup;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Localization {

    /// <summary>Extensión de XAML para textos traducidos: Text="{loc:T Clave}".</summary>
    [MarkupExtensionReturnType(typeof(string))]
    public class TExtension : MarkupExtension {
        public TExtension() { }
        public TExtension(string key) { Key = key; }

        [ConstructorArgument("key")]
        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Key ?? string.Empty);
    }
}

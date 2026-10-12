using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace AntonioAguadoRovira.NINA.Rgbfocus.RgbfocusDockables {

    /// <summary>
    /// Crea los iconos de la barra de NINA: un marco cuadrado con dos líneas de texto
    /// (como el icono "AF" de NINA, pero con el texto que se le indique).
    /// </summary>
    internal static class IconFactory {

        public static GeometryGroup Create(string top, string bottom) {
            var group = new GeometryGroup { FillRule = FillRule.Nonzero };

            // Marco de esquinas redondeadas (100x100) con trazo fino: rectángulo exterior menos el interior
            var outer = new RectangleGeometry(new Rect(0, 0, 100, 100), 14, 14);
            var inner = new RectangleGeometry(new Rect(5, 5, 90, 90), 10, 10);
            group.Children.Add(new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner));

            // Dos líneas de texto con aire alrededor y entre ellas
            AddLine(group, top, 21, 26);
            AddLine(group, bottom, 53, 26);

            if (group.CanFreeze) group.Freeze();
            return group;
        }

        /// <summary>Añade una línea de texto centrada horizontalmente en un recuadro de altura <paramref name="height"/>.</summary>
        private static void AddLine(GeometryGroup group, string text, double y, double height) {
            const double maxWidth = 62;
            var ft = new FormattedText(
                text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                100, Brushes.White, 1.0);
            var geo = ft.BuildGeometry(new Point(0, 0));
            var b = geo.Bounds;
            if (b.IsEmpty || b.Width <= 0 || b.Height <= 0) return;

            // Se escala el texto para que quepa y se centra en su recuadro
            double scale = Math.Min(maxWidth / b.Width, height / b.Height);
            double w = b.Width * scale, h = b.Height * scale;
            double x = (100 - w) / 2, yy = y + (height - h) / 2;
            var m = new Matrix();
            m.Translate(-b.X, -b.Y);
            m.Scale(scale, scale);
            m.Translate(x, yy);
            var path = geo.GetFlattenedPathGeometry();
            path.Transform = new MatrixTransform(m);
            group.Children.Add(path);
        }
    }
}

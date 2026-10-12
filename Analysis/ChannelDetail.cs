using NINA.Core.Utility;
using NINA.Image.ImageAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Analysis {

    /// <summary>
    /// Resultado detallado de un canal: estadísticas de NINA, lista de estrellas y una vista previa
    /// (reducida y ya estirada) de la imagen que se analizó.
    /// </summary>
    public sealed class ChannelDetail {
        private const int PreviewMaxWidth = 3000;

        public string Name { get; set; }
        public double Hfr { get; set; } = double.NaN;
        public double MedianHfr { get; set; } = double.NaN;
        public double HfrStdDev { get; set; } = double.NaN;
        public int Stars { get; set; }

        /// <summary>Tamaño de la imagen original (los centros de las estrellas están en estas coordenadas).</summary>
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

        /// <summary>Estrellas con HFR válido: las que realmente entran en la media y la desviación.</summary>
        public int ValidStars { get; set; }

        /// <summary>σ relativa: desviación del HFR entre estrellas dividida por el HFR medio (fracción, 0.13 = 13 %).</summary>
        public double RelativeSigma => Hfr > 0 && !double.IsNaN(HfrStdDev) ? HfrStdDev / Hfr : double.NaN;

        /// <summary>Error típico de la media del HFR: σ / √N.</summary>
        public double StdError => ValidStars > 0 && !double.IsNaN(HfrStdDev) ? HfrStdDev / Math.Sqrt(ValidStars) : double.NaN;

        // ---- Mapa de zonas (rejilla 3x3, de izquierda a derecha y de arriba a abajo) ----
        public const int ZoneGrid = 3;
        public double[] ZoneHfr { get; } = Enumerable.Repeat(double.NaN, ZoneGrid * ZoneGrid).ToArray();
        public double[] ZoneMean { get; } = Enumerable.Repeat(double.NaN, ZoneGrid * ZoneGrid).ToArray();
        public double[] ZoneStdDev { get; } = Enumerable.Repeat(double.NaN, ZoneGrid * ZoneGrid).ToArray();
        public int[] ZoneStars { get; } = new int[ZoneGrid * ZoneGrid];
        public double ZoneMin { get; set; } = double.NaN;
        public double ZoneMax { get; set; } = double.NaN;

        /// <summary>HFR de la mitad derecha menos el de la izquierda (positivo: la derecha está más desenfocada).</summary>
        public double TiltX { get; set; } = double.NaN;

        /// <summary>Error típico aproximado de TiltX, TiltY y Curvature (permite distinguir una diferencia real del ruido).</summary>
        public double TiltXError { get; set; } = double.NaN;
        public double TiltYError { get; set; } = double.NaN;
        public double CurvatureError { get; set; } = double.NaN;

        /// <summary>HFR de la fila inferior menos el de la superior.</summary>
        public double TiltY { get; set; } = double.NaN;

        /// <summary>HFR de las esquinas menos el del centro (positivo: las esquinas son peores, curvatura de campo o coma).</summary>
        public double Curvature { get; set; } = double.NaN;

        private const int MinStarsPerZone = 5;
        private const int MinStarsPerGroup = 10;

        public List<DetectedStar> StarList { get; set; } = new List<DetectedStar>();

        /// <summary>Vista previa congelada (Gray8 o Bgr24), apta para usarse desde el hilo de UI.</summary>
        public BitmapSource Preview { get; set; }

        /// <summary>Relación entre píxeles de la vista previa y píxeles de la imagen original.</summary>
        public double PreviewScale { get; set; } = 1.0;

        public static ChannelDetail From(string name, StarDetectionResult r, BitmapSource stretchedImage, int imageWidth, int imageHeight) {
            var d = new ChannelDetail { Name = name, ImageWidth = imageWidth, ImageHeight = imageHeight };
            if (r != null) {
                d.Hfr = r.AverageHFR;
                d.HfrStdDev = r.HFRStdDev;
                d.Stars = r.DetectedStars;
                d.StarList = r.StarList ?? new List<DetectedStar>();

                var hfrs = d.StarList.Select(s => s.HFR).Where(v => !double.IsNaN(v)).ToList();
                d.ValidStars = hfrs.Count;
                d.MedianHfr = Median(hfrs);
                d.ComputeZones();
            }

            d.Preview = CreatePreview(stretchedImage, out double scale);
            d.PreviewScale = scale;
            return d;
        }

        private static double Median(List<double> values) {
            if (values == null || values.Count == 0) return double.NaN;
            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;
            return n % 2 == 1 ? sorted[n / 2] : 0.5 * (sorted[n / 2 - 1] + sorted[n / 2]);
        }

        private static double MedianIfEnough(List<double> values, int minCount) =>
            values.Count >= minCount ? Median(values) : double.NaN;

        /// <summary>Mediana y error típico aproximado (σ / √N) de un grupo de estrellas.</summary>
        private static (double Median, double Error) Group(List<double> values, int minCount) {
            if (values.Count < minCount) return (double.NaN, double.NaN);
            double mean = values.Average();
            double variance = values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1);
            return (Median(values), Math.Sqrt(variance / values.Count));
        }

        private static (double Diff, double Error) Difference((double Median, double Error) a, (double Median, double Error) b) =>
            (a.Median - b.Median, Math.Sqrt(a.Error * a.Error + b.Error * b.Error));

        /// <summary>
        /// Reparte las estrellas en una rejilla 3x3 según la posición de su centro y calcula el HFR (mediana)
        /// de cada zona, además de la inclinación (derecha-izquierda, abajo-arriba) y la curvatura (esquinas-centro).
        /// </summary>
        private void ComputeZones() {
            if (ImageWidth <= 0 || ImageHeight <= 0) return;

            int cells = ZoneGrid * ZoneGrid;
            var zones = new List<double>[cells];
            for (int i = 0; i < cells; i++) zones[i] = new List<double>();
            var left = new List<double>(); var right = new List<double>();
            var top = new List<double>(); var bottom = new List<double>();
            var center = new List<double>(); var corners = new List<double>();

            foreach (var star in StarList) {
                if (double.IsNaN(star.HFR)) continue;
                var box = star.BoundingBox;
                double cx = box.X + box.Width / 2.0;
                double cy = box.Y + box.Height / 2.0;
                int col = Math.Max(0, Math.Min(ZoneGrid - 1, (int)(cx / ImageWidth * ZoneGrid)));
                int row = Math.Max(0, Math.Min(ZoneGrid - 1, (int)(cy / ImageHeight * ZoneGrid)));

                zones[row * ZoneGrid + col].Add(star.HFR);
                if (col == 0) left.Add(star.HFR);
                if (col == ZoneGrid - 1) right.Add(star.HFR);
                if (row == 0) top.Add(star.HFR);
                if (row == ZoneGrid - 1) bottom.Add(star.HFR);
                if (row == 1 && col == 1) center.Add(star.HFR);
                if ((row == 0 || row == ZoneGrid - 1) && (col == 0 || col == ZoneGrid - 1)) corners.Add(star.HFR);
            }

            for (int i = 0; i < cells; i++) {
                ZoneStars[i] = zones[i].Count;
                ZoneHfr[i] = MedianIfEnough(zones[i], MinStarsPerZone);
                if (zones[i].Count >= MinStarsPerZone) {
                    double mean = zones[i].Average();
                    ZoneMean[i] = mean;
                    ZoneStdDev[i] = Math.Sqrt(zones[i].Sum(v => (v - mean) * (v - mean)) / (zones[i].Count - 1));
                }
            }

            var valid = ZoneHfr.Where(v => !double.IsNaN(v)).ToList();
            if (valid.Count > 0) {
                ZoneMin = valid.Min();
                ZoneMax = valid.Max();
            }

            var tx = Difference(Group(right, MinStarsPerGroup), Group(left, MinStarsPerGroup));
            var ty = Difference(Group(bottom, MinStarsPerGroup), Group(top, MinStarsPerGroup));
            var cv = Difference(Group(corners, MinStarsPerGroup), Group(center, MinStarsPerGroup));
            TiltX = tx.Diff; TiltXError = tx.Error;
            TiltY = ty.Diff; TiltYError = ty.Error;
            Curvature = cv.Diff; CurvatureError = cv.Error;
        }

        /// <summary>
        /// Reduce la imagen estirada y copia los píxeles a un bitmap nuevo. Así no se mantiene en memoria
        /// la imagen completa (un Rgb48 de 24 MP ocupa unos 150 MB).
        /// </summary>
        private static BitmapSource CreatePreview(BitmapSource src, out double scale) {
            scale = 1.0;
            if (src == null) return null;
            try {
                BitmapSource work = src;
                if (src.PixelWidth > PreviewMaxWidth) {
                    scale = (double)PreviewMaxWidth / src.PixelWidth;
                    work = new TransformedBitmap(src, new ScaleTransform(scale, scale));
                }

                var target = work.Format == PixelFormats.Gray16 || work.Format == PixelFormats.Gray8
                    ? PixelFormats.Gray8
                    : PixelFormats.Bgr24;
                var converted = new FormatConvertedBitmap(work, target, null, 0);

                int width = converted.PixelWidth;
                int height = converted.PixelHeight;
                int stride = (width * converted.Format.BitsPerPixel + 7) / 8;
                var pixels = new byte[stride * height];
                converted.CopyPixels(pixels, stride, 0);

                var bmp = BitmapSource.Create(width, height, 96, 96, converted.Format, null, pixels, stride);
                bmp.Freeze();
                return bmp;
            } catch (Exception ex) {
                Logger.Error($"RGB HFR Analyzer: could not create the preview: {ex}");
                scale = 1.0;
                return null;
            }
        }

        /// <summary>Dibuja un círculo sobre cada estrella detectada. Debe llamarse desde el hilo de UI.</summary>
        public BitmapSource RenderAnnotated() {
            if (Preview == null) return null;

            int w = Preview.PixelWidth;
            int h = Preview.PixelHeight;
            var pen = new Pen(Brushes.Yellow, 1.0);
            pen.Freeze();

            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen()) {
                dc.DrawImage(Preview, new Rect(0, 0, w, h));
                foreach (var s in StarList) {
                    var box = s.BoundingBox;
                    double cx = (box.X + box.Width / 2.0) * PreviewScale;
                    double cy = (box.Y + box.Height / 2.0) * PreviewScale;
                    double radius = Math.Max(box.Width, box.Height) / 2.0 * PreviewScale + 3.0;
                    dc.DrawEllipse(null, pen, new Point(cx, cy), radius, radius);
                }
            }

            var rtb = new RenderTargetBitmap(w, h, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);
            rtb.Freeze();
            return rtb;
        }
    }
}

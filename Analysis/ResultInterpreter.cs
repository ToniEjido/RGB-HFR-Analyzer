using AntonioAguadoRovira.NINA.Rgbfocus.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Analysis {

    /// <summary>
    /// Detecta problemas que merecen atención tras analizar una imagen. Devuelve una cadena vacía si no hay
    /// nada importante, para que el panel no muestre ningún aviso.
    /// Los umbrales están al principio y son orientativos.
    /// </summary>
    public static class ResultInterpreter {
        // Una diferencia solo se considera real si supera este número de veces su error típico
        private const double Significance = 3.0;

        // Umbrales para avisar (fracción del HFR)
        private const double TiltWarning = 0.10;             // inclinación de más de un 10 %
        private const double CurvatureWarning = 0.20;        // esquinas más de un 20 % peores que el centro
        private const double InvertedCurvatureWarning = 0.10;
        private const int MinStarsWarning = 100;             // menos estrellas que esto: medidas poco fiables
        private const double SigmaWarning = 0.35;            // σ relativa alta

        private sealed class Channel {
            public string Name;
            public ChannelDetail D;
        }

        public static string Build(ChannelDetail r, ChannelDetail g, ChannelDetail b, ChannelDetail rgb) {
            var channels = new List<Channel> {
                new Channel { Name = "R", D = r },
                new Channel { Name = "G", D = g },
                new Channel { Name = "B", D = b },
                new Channel { Name = "RGB", D = rgb }
            }.Where(c => c.D != null && !double.IsNaN(c.D.Hfr) && c.D.ValidStars > 0).ToList();

            if (channels.Count == 0) return string.Empty;

            var warnings = new List<string>();
            AddIfNotNull(warnings, Tilt(channels, horizontal: true));
            AddIfNotNull(warnings, Tilt(channels, horizontal: false));
            AddIfNotNull(warnings, Curvature(channels));
            AddIfNotNull(warnings, FewStars(channels));
            AddIfNotNull(warnings, HighDispersion(channels));
            return string.Join("\n", warnings);
        }

        private static void AddIfNotNull(List<string> list, string text) {
            if (!string.IsNullOrEmpty(text)) list.Add(text);
        }

        private static double Overall(ChannelDetail d) => double.IsNaN(d.MedianHfr) ? d.Hfr : d.MedianHfr;

        private static bool IsSignificant(double value, double error) =>
            !double.IsNaN(value) && (double.IsNaN(error) || error <= 0 || Math.Abs(value) >= Significance * error);

        // ------------------------------------------------------------------ inclinación

        private static string Tilt(List<Channel> channels, bool horizontal) {
            var flagged = new List<(string Name, double Rel, int Sign)>();
            foreach (var c in channels) {
                double tilt = horizontal ? c.D.TiltX : c.D.TiltY;
                double error = horizontal ? c.D.TiltXError : c.D.TiltYError;
                double overall = Overall(c.D);
                if (overall <= 0 || !IsSignificant(tilt, error)) continue;
                double rel = Math.Abs(tilt) / overall;
                if (rel >= TiltWarning) flagged.Add((c.Name, rel, tilt > 0 ? 1 : -1));
            }
            if (flagged.Count == 0) return null;

            string where = string.Join(", ", flagged.Select(f => f.Name));
            double max = flagged.Max(f => f.Rel) * 100;
            string maxText = max.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
            bool sameSide = flagged.Select(f => f.Sign).Distinct().Count() == 1;

            string sentence;
            if (sameSide) {
                string worse = flagged[0].Sign > 0 ? (horizontal ? Loc.T("W.Right") : Loc.T("W.Bottom")) : (horizontal ? Loc.T("W.Left") : Loc.T("W.Top"));
                string better = flagged[0].Sign > 0 ? (horizontal ? Loc.T("W.Left") : Loc.T("W.Top")) : (horizontal ? Loc.T("W.Right") : Loc.T("W.Bottom"));
                sentence = Loc.T("W.TiltSame", worse, maxText, better);
            } else {
                sentence = Loc.T("W.TiltDiff", maxText, horizontal ? Loc.T("W.SidesLR") : Loc.T("W.SidesTB"));
            }

            string origin = sameSide && flagged.Count >= 3 ? Loc.T("W.TiltMech") : Loc.T("W.TiltCheck");
            return Loc.T("W.TiltHead", max >= 20 ? Loc.T("W.Marked") : Loc.T("W.Moderate"), where, sentence, origin);
        }

        // ------------------------------------------------------------------ curvatura de campo

        private static string Curvature(List<Channel> channels) {
            var curved = new List<(string Name, double Rel)>();
            var inverted = new List<(string Name, double Rel)>();

            foreach (var c in channels) {
                double overall = Overall(c.D);
                if (overall <= 0 || !IsSignificant(c.D.Curvature, c.D.CurvatureError)) continue;
                double rel = c.D.Curvature / overall;
                if (rel >= CurvatureWarning) curved.Add((c.Name, rel));
                else if (rel <= -InvertedCurvatureWarning) inverted.Add((c.Name, -rel));
            }

            var parts = new List<string>();
            if (curved.Count > 0) {
                parts.Add(Loc.T("W.Curv", string.Join(", ", curved.Select(x => x.Name)),
                                (curved.Max(x => x.Rel) * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture)));
            }
            if (inverted.Count > 0) {
                parts.Add(Loc.T("W.CurvInv", string.Join(", ", inverted.Select(x => x.Name)),
                                (inverted.Max(x => x.Rel) * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture)));
            }
            return parts.Count == 0 ? null : string.Join("\n", parts);
        }

        // ------------------------------------------------------------------ fiabilidad

        private static string FewStars(List<Channel> channels) {
            var few = channels.Where(c => c.D.ValidStars < MinStarsWarning).ToList();
            if (few.Count == 0) return null;
            string detail = string.Join(", ", few.Select(c => $"{c.Name}: {c.D.ValidStars}"));
            return Loc.T("W.FewStars", detail);
        }

        private static string HighDispersion(List<Channel> channels) {
            var high = channels.Where(c => !double.IsNaN(c.D.RelativeSigma) && c.D.RelativeSigma > SigmaWarning).ToList();
            if (high.Count == 0) return null;
            string detail = string.Join(", ", high.Select(c => $"{c.Name}: {c.D.RelativeSigma * 100:0} %"));
            return Loc.T("W.HighDisp", detail);
        }
    }
}

using System.Collections.Generic;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Localization {

    /// <summary>Textos de la interfaz en inglés y español. Cada clave lleva sus dos versiones juntas.</summary>
    internal static class Strings {
        internal static readonly Dictionary<string, string> En = new Dictionary<string, string>();
        internal static readonly Dictionary<string, string> Es = new Dictionary<string, string>();

        private static void A(string key, string en, string es) {
            En[key] = en;
            Es[key] = es;
        }

        static Strings() {
            A("Panel.Title", "RGB HFR", "HFR RGB");
            A("Status.Waiting", "Waiting for a colour image...", "Esperando una imagen en color...");
            A("Status.NotColor", "The last image is not a colour image (no Bayer mosaic).", "La última imagen no es de color (no hay mosaico Bayer).");
            A("Status.Analyzing", "Analysing...", "Analizando...");
            A("Status.FewStars", "Too few usable stars ({0}).", "Pocas estrellas utilizables ({0}).");
            A("Status.Error", "Error analysing the image (see the NINA log).", "Error al analizar la imagen (ver el log de NINA).");
            A("Status.Mixing", "Calculating the {0} mix...", "Calculando la mezcla {0}...");
            A("Info.Calculating", "{0}: calculating...", "{0}: calculando...");
            A("Info.MixUnavailable", "{0}: this mix was not calculated when the image was captured.", "{0}: esta mezcla no se calculó al capturar la imagen.");
            A("Info.CaptureUnknown", "Captured: date not available", "Captura: fecha no disponible");
            A("Info.Capture", "Captured: {0}", "Captura: {0}");
            A("Info.Showing", "Showing: {0}", "Mostrando: {0}");
            A("Info.Hfr", "HFR: {0}  (median {1}, σ {2})", "HFR: {0}  (mediana {1}, σ {2})");
            A("Info.Stars", "Stars detected: {0} ★", "Estrellas detectadas: {0} ★");
            A("Info.ChannelSummary", "Summary by channel (HFR · stars)", "Resumen por canal (HFR · estrellas)");
            A("Diag.RelSigma", "Relative σ: {0}", "σ relativa: {0}");
            A("Diag.StdError", "Error of the mean: {0}  ({1} valid stars)", "Error de la media: {0}  ({1} estrellas válidas)");
            A("Diag.Zones", "Zones (median): {0}", "Zonas (mediana): {0}");
            A("Diag.Tilt", "Tilt left→right: {0}   top→bottom: {1}", "Inclinación izq→der: {0}   arriba→abajo: {1}");
            A("Diag.Curvature", "Curvature (corners − centre): {0}", "Curvatura (esquinas − centro): {0}");
            A("Zone.TL", "Top left", "Arriba izquierda");
            A("Zone.T", "Top centre", "Arriba centro");
            A("Zone.TR", "Top right", "Arriba derecha");
            A("Zone.L", "Middle left", "Centro izquierda");
            A("Zone.C", "Centre", "Centro");
            A("Zone.R", "Middle right", "Centro derecha");
            A("Zone.BL", "Bottom left", "Abajo izquierda");
            A("Zone.B", "Bottom centre", "Abajo centro");
            A("Zone.BR", "Bottom right", "Abajo derecha");
            A("Zone.Few", "{0}\nFew stars ({1}): not calculated", "{0}\nPocas estrellas ({1}): no se calcula");
            A("Zone.Tip", "{0}  ({1})\nHFR (median): {2}\nHFR (mean): {3}\nσ: {4}  ({5} % of the HFR)\nError of the mean: ±{6}\nStars: {7} ★\nVs. global median ({8}): {9} %", "{0}  ({1})\nHFR (mediana): {2}\nHFR (media): {3}\nσ: {4}  ({5} % del HFR)\nError de la media: ±{6}\nEstrellas: {7} ★\nRespecto a la mediana global ({8}): {9} %");
            A("Btn.Warnings", "Warnings", "Avisos");
            A("Btn.WarningsN", "Warnings ({0})", "Avisos ({0})");
            A("Btn.Zones", "Zones", "Zonas");
            A("Btn.Stars", "Stars", "Estrellas");
            A("Btn.Mix", "Mix", "Mezclar");
            A("Tip.Warnings", "Shows or hides the important warnings from the analysis (tilt, field curvature, few stars, high dispersion).", "Muestra u oculta los avisos importantes del análisis (inclinación, curvatura de campo, pocas estrellas, dispersión alta).");
            A("Tip.Zones", "Overlays a 3x3 grid with the HFR of each zone. Hover over a zone to see its statistics.", "Superpone una rejilla de 3x3 con el HFR de cada zona. Pasa el ratón por una zona para ver sus estadísticas.");
            A("Tip.Stars", "Draws a circle around each star detected in the channel shown.", "Dibuja un círculo sobre cada estrella detectada en el canal mostrado.");
            A("Tip.Mix", "Lets you select several channels at once to analyse their mix (average). When off, each button selects a single channel. The RGB button always selects all three.", "Permite marcar varios canales a la vez para analizar su mezcla (promedio). Apagado, cada botón elige un solo canal. El botón RGB marca los tres siempre.");
            A("Tip.Channel", "Channel {0}: {1} stars detected", "Canal {0}: {1} estrellas detectadas");
            A("Tip.ZoomIn", "Zoom in", "Acercar");
            A("Tip.ZoomOut", "Zoom out", "Alejar");
            A("Tip.Fit", "Fit to window", "Ajustar a la ventana");
            A("Tip.OneToOne", "Actual size (100 %)", "Tamaño real (100 %)");
            A("AF.Title", "RGB Autofocus", "RGB Autoenfoque");
            A("AF.ChannelLabel", "Channel to focus:", "Canal a enfocar:");
            A("AF.ChannelTip", "Channel the HFR is taken from to focus. RGB uses the whole image, like NINA's normal autofocus. R+G, G+B and R+B use the average of two channels (focus between them).", "Canal del que se toma el HFR para enfocar. RGB usa la imagen completa, como el autoenfoque normal de NINA. R+G, G+B y R+B usan el promedio de dos canales (enfoque intermedio entre ambos).");
            A("AF.Start", "Start autofocus", "Iniciar autoenfoque");
            A("AF.Cancel", "Cancel", "Cancelar");
            A("AF.GlobalLabel", "Use this channel in all NINA autofocus runs (including the sequencer)", "Usar este canal en todos los autoenfoques de NINA (incluido el secuenciador)");
            A("AF.GlobalTip", "Leaves this plugin's star detector selected in NINA, so any autofocus (manual, legacy or advanced sequencer, time, temperature or HFR triggers) measures the channel chosen above. The HFR NINA shows for each image is not affected. Turning it off restores the previous detector.", "Deja seleccionado el detector del plugin en NINA, de modo que cualquier autoenfoque (manual, secuenciador heredado o avanzado, disparadores por tiempo, temperatura o HFR) mida el canal elegido arriba. El HFR que NINA muestra de cada imagen no se ve afectado. Al desactivarlo se devuelve el detector anterior.");
            A("AF.GlobalHint", "Active: every NINA autofocus (including sequencer ones) measures channel {0}.", "Activo: todos los autoenfoques de NINA (también los del secuenciador) miden el canal {0}.");
            A("AF.Table", "Best position found per channel (one row per focused channel)", "Mejor posición encontrada por canal (una fila por canal enfocado)");
            A("AF.ColChannel", "Channel", "Canal");
            A("AF.ColPosition", "Position", "Posición");
            A("AF.ColHfr", "HFR", "HFR");
            A("AF.PositionTip", "Focuser position", "Posición del enfocador");
            A("AF.HfrTip", "HFR at the best point", "HFR en el mejor punto");
            A("AF.Go", "Go", "Ir");
            A("AF.GoTip", "Move the focuser to this position", "Mover el enfocador a esta posición");
            A("AF.NotColor", "Channel {0} cannot be measured: the camera or the image is not colour (Bayer). Choose RGB, or use a colour camera.",
                             "No se puede medir el canal {0}: la cámara o la imagen no es de color (Bayer). Elige RGB, o usa una cámara de color.");
            A("AF.Binned", "Channel {0} cannot be measured with binning {1}x{2}: binning mixes neighbouring pixels and destroys the colour pattern. Use binning 1x1 for autofocus, or choose RGB.",
                           "No se puede medir el canal {0} con binning {1}x{2}: el binning mezcla píxeles vecinos y destruye el patrón de color. Usa binning 1x1 en el autoenfoque, o elige RGB.");
            A("AF.BadChannel", "Unknown channel: {0}.", "Canal desconocido: {0}.");
            A("AF.RunInProgress", "Another channel autofocus is already running.", "Ya hay otro autoenfoque por canal en marcha.");
            A("AF.NoDetector", "The plugin's star detector was not found.", "No se encontró el detector de estrellas del plugin.");
            A("AF.NoCamera", "The camera is not connected.", "La cámara no está conectada.");
            A("AF.NoFocuser", "The focuser is not connected.", "El enfocador no está conectado.");
            A("AF.CameraBusy", "The camera is busy.", "La cámara está ocupada.");
            A("AF.Cancelled", "Autofocus cancelled.", "Autoenfoque cancelado.");
            A("AF.Error", "Autofocus error (see the NINA log).", "Error en el autoenfoque (ver el log de NINA).");
            A("AF.Moving", "Moving the focuser to {0}…", "Moviendo el enfocador a {0}…");
            A("AF.Moved", "Focuser at {0}.", "Enfocador en {0}.");
            A("AF.MoveError", "Could not move the focuser (see the NINA log).", "No se pudo mover el enfocador (ver el log de NINA).");
            A("Seq.ChannelToFocus", "Channel to focus", "Canal a enfocar");
            A("Seq.Plan", "Channel plan", "Plan de canales");
            A("Seq.Repeat", "Repeat the plan", "Repetir el plan");
            A("Seq.RepeatTip", "When the last step ends, go back to the first", "Al terminar el último tramo, vuelve al primero");
            A("Seq.FocusAtStart", "Focus at start", "Enfocar al empezar");
            A("Seq.FocusAtStartTip", "Runs an autofocus on the first channel before the first exposure", "Ejecuta un autoenfoque del primer canal antes de la primera exposición");
            A("Seq.AddStepTip", "Add a step to the plan", "Añadir un tramo al plan");
            A("Seq.RemoveStepTip", "Remove this step", "Quitar este tramo");
            A("Seq.Channel", "Channel", "Canal");
            A("Seq.For", "for", "durante");
            A("Seq.ExposuresThen", "exposures, then refocus on the next one", "exposiciones, y después reenfoca el siguiente");
            A("Seq.PlanDone", "Plan completed", "Plan completado");
            A("Seq.NoCamera", "The camera is not connected.", "La cámara no está conectada.");
            A("Seq.NoFocuser", "The focuser is not connected.", "El enfocador no está conectado.");
            A("Opt.Language", "Language", "Idioma");
            A("Opt.Auto", "Automatic (follow NINA)", "Automático (el de NINA)");
            A("Opt.Restart", "Restart NINA to apply the language change.", "Reinicia NINA para aplicar el cambio de idioma.");
            A("Pattern.Desc", "Channel of the last RGB HFR Analyzer autofocus (R, G, B, RGB, RG, GB, RB)", "Canal del último autoenfoque de RGB HFR Analyzer (R, G, B, RGB, RG, GB, RB)");
            A("W.Marked", "Marked", "marcada");
            A("W.Moderate", "Moderate", "moderada");
            A("W.TiltHead", "⚠ {0} tilt ({1}): {2}.{3}", "⚠ Inclinación {0} ({1}): {2}.{3}");
            A("W.Right", "right", "derecho");
            A("W.Left", "left", "izquierdo");
            A("W.Bottom", "bottom", "inferior");
            A("W.Top", "top", "superior");
            A("W.TiltSame", "The {0} side is up to {1} % more out of focus than the {2} side", "El lado {0} está hasta un {1} % más desenfocado que el {2}");
            A("W.TiltDiff", "Difference of up to {0} % between {1}, in a different direction depending on the channel", "Diferencia de hasta un {0} % entre {1}, en sentido distinto según el canal");
            A("W.SidesLR", "the left and right sides", "los lados izquierdo y derecho");
            A("W.SidesTB", "the top and bottom sides", "los lados superior e inferior");
            A("W.TiltMech", " It repeats in several channels: probable mechanical cause (sensor or adapters not perpendicular to the optical axis).", " Se repite en varios canales: probable causa mecánica (sensor o adaptadores no perpendiculares al eje óptico).");
            A("W.TiltCheck", " Check that the sensor is perpendicular to the optical axis.", " Revisa que el sensor esté perpendicular al eje óptico.");
            A("W.Curv", "⚠ Marked field curvature ({0}): the corners are up to {1} % more out of focus than the centre. A suitable field flattener usually fixes it.", "⚠ Curvatura de campo marcada ({0}): las esquinas están hasta un {1} % más desenfocadas que el centro. Un aplanador de campo adecuado suele corregirlo.");
            A("W.CurvInv", "⚠ Centre more out of focus than the corners ({0}, up to {1} %): unusual; check the focus or the flattener distance.", "⚠ Centro más desenfocado que las esquinas ({0}, hasta un {1} %): poco habitual; revisa el enfoque o la distancia del aplanador.");
            A("W.FewStars", "⚠ Few stars ({0}): the measurements, especially the zone ones, are not reliable.", "⚠ Pocas estrellas ({0}): las medidas, sobre todo las de zonas, no son fiables.");
            A("W.HighDisp", "⚠ High dispersion in star size ({0}): there may be doubles, false detections or a very irregular field.", "⚠ Mucha dispersión en el tamaño de las estrellas ({0}): puede haber dobles, falsas detecciones o un campo muy irregular.");
        }
    }
}

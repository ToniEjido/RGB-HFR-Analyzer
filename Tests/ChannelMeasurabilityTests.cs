using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using NUnit.Framework;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Tests {

    /// <summary>
    /// Punto 2 de la revisión: no anotar un autoenfoque normal como si fuera de un canal. Si el canal no se puede medir
    /// (cámara mono, o cámara de color con binning), hay que fallar y no medir otra cosa en silencio.
    /// </summary>
    [TestFixture]
    public class ChannelMeasurabilityTests {

        [TestCase("R")]
        [TestCase("G")]
        [TestCase("B")]
        [TestCase("R+G")]
        [TestCase("G+B")]
        [TestCase("R+B")]
        public void ColourCamera_NoBinning_CanMeasureEveryChannel(string channel) {
            Assert.That(ChannelMeasurability.Check(channel, isColor: true, binX: 1, binY: 1), Is.EqualTo(MeasurabilityProblem.None));
        }

        [TestCase("R")]
        [TestCase("G")]
        [TestCase("B")]
        [TestCase("R+G")]
        public void MonochromeInput_CannotMeasureAChannel(string channel) {
            Assert.That(ChannelMeasurability.Check(channel, isColor: false, binX: 1, binY: 1), Is.EqualTo(MeasurabilityProblem.NotColor));
        }

        [TestCase("R", 2, 2)]
        [TestCase("G", 2, 2)]
        [TestCase("B", 3, 3)]
        [TestCase("R+B", 4, 4)]
        [TestCase("G", 1, 2)]
        [TestCase("G", 2, 1)]
        public void ColourCameraWithBinning_CannotMeasureAChannel(string channel, int binX, int binY) {
            // Un OSC con binning (por ejemplo 2x2) ya no tiene patrón Bayer: R, G y B no existen por separado
            Assert.That(ChannelMeasurability.Check(channel, isColor: true, binX: binX, binY: binY), Is.EqualTo(MeasurabilityProblem.Binned));
        }

        [TestCase(true, 1, 1)]
        [TestCase(true, 2, 2)]
        [TestCase(false, 1, 1)]
        [TestCase(false, 2, 2)]
        public void Rgb_IsTheNormalDetection_AndIsAlwaysMeasurable(bool isColor, int binX, int binY) {
            Assert.That(ChannelMeasurability.Check("L", isColor, binX, binY), Is.EqualTo(MeasurabilityProblem.None));
        }

        [TestCase("X")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("RGB")] // "RGB" es solo el nombre en pantalla; la clave interna es "L"
        public void UnknownChannel_IsRejected(string channel) {
            Assert.That(ChannelMeasurability.Check(channel, isColor: true, binX: 1, binY: 1), Is.EqualTo(MeasurabilityProblem.InvalidChannel));
        }

        [Test]
        public void TheException_KeepsTheProblemAndTheChannel_SoTheCallerCanReportIt() {
            var ex = new ChannelNotMeasurableException(MeasurabilityProblem.Binned, "G", "message");
            Assert.That(ex.Problem, Is.EqualTo(MeasurabilityProblem.Binned));
            Assert.That(ex.Channel, Is.EqualTo("G"));
            Assert.That(ex.Message, Is.EqualTo("message"));
        }
    }
}

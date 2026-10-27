using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Tests {

    /// <summary>
    /// Punto 1 de la revisión: el canal no puede cambiar durante un autoenfoque, venga el cambio de la interfaz o de otro
    /// elemento del secuenciador, para que una curva de enfoque no mezcle canales.
    /// </summary>
    [TestFixture, NonParallelizable]
    public class FocusChannelStateTests {
        private readonly List<IDisposable> runs = new List<IDisposable>();

        [SetUp]
        public void SetUp() {
            FocusChannelState.TrySelect("G");
        }

        [TearDown]
        public void TearDown() {
            foreach (var r in runs) r.Dispose();
            runs.Clear();
            FocusChannelState.TrySelect("G");
        }

        private IDisposable Begin(string channel) {
            var run = FocusChannelState.BeginRun(channel);
            runs.Add(run);
            return run;
        }

        [Test]
        public void Select_WhenIdle_ChangesTheActiveChannel() {
            Assert.That(FocusChannelState.TrySelect("B"), Is.True);
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("B"));
            Assert.That(FocusChannelState.SelectedChannel, Is.EqualTo("B"));
        }

        [Test]
        public void Select_InvalidKey_IsRefused() {
            Assert.That(FocusChannelState.TrySelect("X"), Is.False);
            Assert.That(FocusChannelState.TrySelect(null), Is.False);
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("G"));
        }

        [Test]
        public void Run_PinsTheChannel_AgainstSelectionChanges() {
            Begin("R");

            Assert.That(FocusChannelState.IsRunActive, Is.True);
            Assert.That(FocusChannelState.TrySelect("B"), Is.False, "a UI change during the run must be refused");
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("R"));
        }

        [Test]
        public void Run_DoesNotLoseTheUsersOwnSelection() {
            FocusChannelState.TrySelect("B");

            var run = Begin("R"); // por ejemplo, una instrucción del secuenciador
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("R"));
            Assert.That(FocusChannelState.SelectedChannel, Is.EqualTo("B"));

            run.Dispose();
            Assert.That(FocusChannelState.IsRunActive, Is.False);
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("B"));
        }

        [Test]
        public void SecondRun_WhileOneIsActive_IsRefused() {
            Begin("R");

            Assert.Throws<FocusRunInProgressException>(() => FocusChannelState.BeginRun("B"));
            Assert.That(FocusChannelState.ActiveChannel, Is.EqualTo("R"), "the running channel must not be replaced");
        }

        [Test]
        public void InvalidChannel_CannotStartARun() {
            Assert.Throws<ArgumentException>(() => FocusChannelState.BeginRun("X"));
            Assert.That(FocusChannelState.IsRunActive, Is.False);
        }

        [Test]
        public void Dispose_ReleasesTheLock_AndCanBeCalledTwice() {
            var run = Begin("R");
            run.Dispose();
            run.Dispose();

            Assert.That(FocusChannelState.IsRunActive, Is.False);
            Assert.That(FocusChannelState.TrySelect("B"), Is.True);
            Assert.DoesNotThrow(() => Begin("G"), "a new run can start after the previous one ended");
        }

        [Test]
        public void Events_AreRaised_SoTheDisplayedSelectionCanFollow() {
            int channelChanges = 0, runChanges = 0;
            EventHandler onChannel = (s, e) => channelChanges++;
            EventHandler onRun = (s, e) => runChanges++;
            FocusChannelState.ActiveChannelChanged += onChannel;
            FocusChannelState.RunStateChanged += onRun;
            try {
                var run = Begin("R");   // G -> R
                run.Dispose();          // R -> G
                Assert.That(channelChanges, Is.EqualTo(2));
                Assert.That(runChanges, Is.EqualTo(2));

                var same = Begin("G");  // el canal activo no cambia, pero sí el estado de la ejecución
                same.Dispose();
                Assert.That(channelChanges, Is.EqualTo(2));
                Assert.That(runChanges, Is.EqualTo(4));
            } finally {
                FocusChannelState.ActiveChannelChanged -= onChannel;
                FocusChannelState.RunStateChanged -= onRun;
            }
        }

        [Test]
        public void ChannelRead_ByTheDetector_NeverMixesChannels_WhileTheUiKeepsChangingIt() {
            Begin("R");
            var seen = new System.Collections.Concurrent.ConcurrentBag<string>();
            using (var stop = new CancellationTokenSource()) {
                var ui = Task.Run(() => {
                    string[] keys = { "G", "B", "L", "R+G", "G+B", "R+B" };
                    int i = 0;
                    while (!stop.IsCancellationRequested) FocusChannelState.TrySelect(keys[i++ % keys.Length]);
                });
                for (int measurement = 0; measurement < 20000; measurement++) seen.Add(FocusChannelState.ActiveChannel);
                stop.Cancel();
                ui.Wait();
            }

            Assert.That(seen, Is.All.EqualTo("R"));
        }
    }
}

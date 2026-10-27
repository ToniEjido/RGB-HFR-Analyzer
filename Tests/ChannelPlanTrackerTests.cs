using AntonioAguadoRovira.NINA.Rgbfocus.Autofocus;
using NUnit.Framework;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Tests {

    /// <summary>
    /// Punto 3 de la revisión: el disparador no debe empezar un autoenfoque que no puede terminar antes del volteo meridiano.
    /// Estas pruebas cubren la regla (cuándo se pospone, qué estado conserva y cuándo se consulta el volteo); que la consulta
    /// use ItemUtility.IsTooCloseToMeridianFlip de NINA se comprueba en el propio disparador.
    /// </summary>
    [TestFixture]
    public class ChannelPlanTrackerTests {
        private static readonly int[] TwoSteps = { 3, 3 };

        private static bool FlipFar() => false;
        private static bool FlipClose() => true;

        [Test]
        public void FocusAtStart_WithTheFlipFar_Focuses() {
            var plan = new ChannelPlanTracker();
            var e = plan.Evaluate(0, TwoSteps, repeat: true, focusAtStart: true, FlipFar);
            Assert.That(e.Decision, Is.EqualTo(PlanDecision.Focus));
        }

        [Test]
        public void FocusAtStart_WithTheFlipTooClose_IsPostponed_AndRunsLater() {
            var plan = new ChannelPlanTracker();

            var first = plan.Evaluate(0, TwoSteps, true, true, FlipClose);
            Assert.That(first.Decision, Is.EqualTo(PlanDecision.PostponedMeridianFlip));
            Assert.That(plan.Started, Is.False, "a postponed autofocus must not start the plan");

            var later = plan.Evaluate(0, TwoSteps, true, true, FlipFar);
            Assert.That(later.Decision, Is.EqualTo(PlanDecision.Focus));
        }

        [Test]
        public void ChannelChange_WithTheFlipTooClose_IsPostponed_WithoutAdvancingThePlan() {
            var plan = new ChannelPlanTracker();
            plan.Advance(step: 0, lights: 0);

            var due = plan.Evaluate(3, TwoSteps, true, true, FlipClose);

            Assert.That(due.Decision, Is.EqualTo(PlanDecision.PostponedMeridianFlip));
            Assert.That(plan.CurrentStep, Is.EqualTo(0));
            Assert.That(plan.BaseCount, Is.EqualTo(0));
            Assert.That(plan.NextStep(2), Is.EqualTo(1), "the channel change is still pending");
        }

        [Test]
        public void ChannelChange_PostponedForTheFlip_StillHappensOnceItIsNotTooClose() {
            var plan = new ChannelPlanTracker();
            plan.Advance(0, 0);

            Assert.That(plan.Evaluate(3, TwoSteps, true, true, FlipClose).Decision, Is.EqualTo(PlanDecision.PostponedMeridianFlip));
            Assert.That(plan.Evaluate(4, TwoSteps, true, true, FlipClose).Decision, Is.EqualTo(PlanDecision.PostponedMeridianFlip));
            Assert.That(plan.Evaluate(5, TwoSteps, true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Focus));
        }

        [Test]
        public void TheFlip_IsOnlyChecked_WhenAnAutofocusIsAboutToRun() {
            var plan = new ChannelPlanTracker();
            plan.Advance(0, 0);
            int checks = 0;
            bool Counting() { checks++; return false; }

            plan.Evaluate(1, TwoSteps, true, true, Counting); // a mitad de tramo: no toca enfocar
            plan.Evaluate(2, TwoSteps, true, true, Counting);
            Assert.That(checks, Is.EqualTo(0));

            plan.Evaluate(3, TwoSteps, true, true, Counting); // fin de tramo: sí
            Assert.That(checks, Is.EqualTo(1));
        }

        [Test]
        public void AFailedAutofocus_LeavesThePlanAsItWas_SoItIsRetried() {
            var plan = new ChannelPlanTracker();
            plan.Advance(0, 0);

            Assert.That(plan.Evaluate(3, TwoSteps, true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Focus));
            // el autoenfoque falla: no se llama a Advance
            Assert.That(plan.Evaluate(4, TwoSteps, true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Focus));
            Assert.That(plan.CurrentStep, Is.EqualTo(0));
        }

        [Test]
        public void Advance_MovesToTheNextStep_AndRepeatWrapsToTheFirst() {
            var plan = new ChannelPlanTracker();
            Assert.That(plan.NextStep(2), Is.EqualTo(0));

            plan.Advance(0, 0);
            Assert.That(plan.NextStep(2), Is.EqualTo(1));

            plan.Advance(1, 3);
            Assert.That(plan.NextStep(2), Is.EqualTo(0));
            Assert.That(plan.Evaluate(5, TwoSteps, true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Wait));
            Assert.That(plan.Evaluate(6, TwoSteps, true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Focus));
        }

        [Test]
        public void LastStep_WithoutRepeat_FinishesThePlan_WithoutAskingAboutTheFlip() {
            var plan = new ChannelPlanTracker();
            plan.Advance(1, 3);
            int checks = 0;
            bool Counting() { checks++; return true; }

            var e = plan.Evaluate(6, TwoSteps, repeat: false, focusAtStart: true, Counting);

            Assert.That(e.Decision, Is.EqualTo(PlanDecision.Done));
            Assert.That(plan.Finished, Is.True);
            Assert.That(checks, Is.EqualTo(0));
            Assert.That(plan.Evaluate(9, TwoSteps, false, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Wait));
        }

        [Test]
        public void WithoutInitialFocus_ThePlanStartsWithTheFirstChannel() {
            var plan = new ChannelPlanTracker();

            var e = plan.Evaluate(5, TwoSteps, true, focusAtStart: false, FlipClose);

            Assert.That(e.Decision, Is.EqualTo(PlanDecision.Wait));
            Assert.That(e.StartedWithoutFocus, Is.True);
            Assert.That(e.Step, Is.EqualTo(0));
            Assert.That(plan.BaseCount, Is.EqualTo(5));
        }

        [Test]
        public void EmptyingTheImageHistory_StartsANewSession() {
            var plan = new ChannelPlanTracker();
            plan.Advance(1, 10);

            var e = plan.Evaluate(2, TwoSteps, true, true, FlipFar);

            Assert.That(e.Decision, Is.EqualTo(PlanDecision.Focus));
            Assert.That(plan.Started, Is.False);
            Assert.That(plan.CurrentStep, Is.EqualTo(-1));
        }

        [Test]
        public void Progress_IsReportedForTheCurrentStep_AndCappedAtItsLength() {
            var plan = new ChannelPlanTracker();
            plan.Advance(0, 0);

            Assert.That(plan.Evaluate(2, TwoSteps, true, true, FlipFar).Taken, Is.EqualTo(2));
            var due = plan.Evaluate(5, TwoSteps, true, true, FlipClose);
            Assert.That(due.Step, Is.EqualTo(0));
            Assert.That(due.Taken, Is.EqualTo(3));
        }

        [Test]
        public void NoSteps_NeverTriggers() {
            var plan = new ChannelPlanTracker();
            Assert.That(plan.Evaluate(10, new int[0], true, true, FlipFar).Decision, Is.EqualTo(PlanDecision.Wait));
        }
    }
}

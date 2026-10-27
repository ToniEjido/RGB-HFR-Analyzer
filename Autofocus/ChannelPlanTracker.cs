using System;
using System.Collections.Generic;

namespace AntonioAguadoRovira.NINA.Rgbfocus.Autofocus {

    /// <summary>Qué debe hacer el disparador del plan de canales antes de la siguiente exposición.</summary>
    public enum PlanDecision {
        /// <summary>Nada: el tramo actual sigue en marcha, o el plan acabó.</summary>
        Wait,
        /// <summary>Enfocar y pasar al siguiente canal ahora.</summary>
        Focus,
        /// <summary>Tocaba enfocar, pero no da tiempo a terminar antes del volteo meridiano: se pospone, sin cambiar el estado del plan.</summary>
        PostponedMeridianFlip,
        /// <summary>El último tramo terminó y el plan no se repite.</summary>
        Done
    }

    /// <summary>Resultado de evaluar el plan: la decisión y el progreso del tramo actual, para mostrarlo.</summary>
    public readonly struct PlanEvaluation {
        public PlanEvaluation(PlanDecision decision, int step, int taken, bool startedWithoutFocus) {
            Decision = decision;
            Step = step;
            Taken = taken;
            StartedWithoutFocus = startedWithoutFocus;
        }
        public PlanDecision Decision { get; }
        /// <summary>Índice del tramo en curso (-1 si el plan aún no ha empezado).</summary>
        public int Step { get; }
        /// <summary>Exposiciones hechas en el tramo en curso.</summary>
        public int Taken { get; }
        /// <summary>El plan acaba de empezar sin autoenfoque inicial: el primer canal ya está activo.</summary>
        public bool StartedWithoutFocus { get; }
    }

    /// <summary>
    /// Estado y reglas del plan de canales del disparador "Channel Change and Refocus", sin dependencias de NINA para poder probarlas.
    /// El disparador le pasa el número de exposiciones LIGHT y si el volteo meridiano está demasiado cerca;
    /// el volteo solo se consulta cuando el plan va a enfocar (igual que los disparadores de autoenfoque de NINA).
    /// </summary>
    public sealed class ChannelPlanTracker {
        public bool Started { get; private set; }
        public bool Finished { get; private set; }
        public int CurrentStep { get; private set; } = -1;
        public int BaseCount { get; private set; }

        /// <param name="lights">Exposiciones LIGHT del historial.</param>
        /// <param name="stepExposures">Exposiciones de cada tramo del plan.</param>
        /// <param name="tooCloseToMeridianFlip">Devuelve true si un autoenfoque ahora no terminaría antes del volteo.</param>
        public PlanEvaluation Evaluate(int lights, IReadOnlyList<int> stepExposures, bool repeat, bool focusAtStart, Func<bool> tooCloseToMeridianFlip) {
            if (stepExposures == null || stepExposures.Count == 0) return new PlanEvaluation(PlanDecision.Wait, -1, 0, false);

            if (lights < BaseCount) {
                // El historial se ha vaciado: empieza una sesión nueva
                Started = false;
                Finished = false;
                CurrentStep = -1;
                BaseCount = 0;
            }
            if (Finished) return new PlanEvaluation(PlanDecision.Wait, -1, 0, false);

            if (!Started) {
                if (focusAtStart) {
                    return new PlanEvaluation(FocusOrPostpone(tooCloseToMeridianFlip), -1, 0, false);
                }
                // Sin autoenfoque inicial: se arranca el plan con el canal del primer tramo ya activo
                Started = true;
                CurrentStep = 0;
                BaseCount = lights;
                return new PlanEvaluation(PlanDecision.Wait, 0, 0, true);
            }

            int step = Math.Min(CurrentStep, stepExposures.Count - 1);
            int taken = lights - BaseCount;
            int shown = Math.Min(taken, stepExposures[step]);

            if (taken < stepExposures[step]) return new PlanEvaluation(PlanDecision.Wait, step, shown, false);

            bool isLast = CurrentStep >= stepExposures.Count - 1;
            if (isLast && !repeat) {
                Finished = true;
                return new PlanEvaluation(PlanDecision.Done, step, shown, false);
            }
            return new PlanEvaluation(FocusOrPostpone(tooCloseToMeridianFlip), step, shown, false);
        }

        private static PlanDecision FocusOrPostpone(Func<bool> tooCloseToMeridianFlip) =>
            tooCloseToMeridianFlip != null && tooCloseToMeridianFlip() ? PlanDecision.PostponedMeridianFlip : PlanDecision.Focus;

        /// <summary>Índice del tramo al que se pasa con el siguiente autoenfoque.</summary>
        public int NextStep(int stepCount) {
            int next = !Started ? 0 : CurrentStep + 1;
            return next >= stepCount ? 0 : next;
        }

        /// <summary>El autoenfoque terminó bien: el plan avanza al tramo indicado. Si falla, no se llama y el estado queda como estaba.</summary>
        public void Advance(int step, int lights) {
            Started = true;
            CurrentStep = step;
            BaseCount = lights;
        }
    }
}

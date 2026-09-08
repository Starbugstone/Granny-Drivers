namespace GrannyRacer.Racing
{
    public enum RaceStartOutcome
    {
        None,
        Skid,
        Boost,
        Turbo
    }

    /// <summary>
    /// Records the first deliberate throttle press during the countdown. Keeping this separate
    /// from the race controller makes the timing rules deterministic and scene-free in tests.
    /// </summary>
    public sealed class RaceStartModel
    {
        private enum PressTiming
        {
            None,
            TooEarly,
            Perfect,
            Late
        }

        private PressTiming pressTiming;
        private bool wasThrottleHeld;

        public void Reset()
        {
            pressTiming = PressTiming.None;
            wasThrottleHeld = false;
        }

        public void Tick(float countdownRemaining, float throttle, float perfectEndsAt,
            float perfectWindow, float throttleThreshold)
        {
            var held = throttle >= throttleThreshold;
            if (pressTiming == PressTiming.None && held && !wasThrottleHeld)
            {
                var perfectStartsAt = perfectEndsAt + perfectWindow;
                if (countdownRemaining > perfectStartsAt) pressTiming = PressTiming.TooEarly;
                else if (countdownRemaining > perfectEndsAt) pressTiming = PressTiming.Perfect;
                else pressTiming = PressTiming.Late;
            }

            wasThrottleHeld = held;
        }

        public RaceStartOutcome Resolve(float throttle, float throttleThreshold)
        {
            if (throttle < throttleThreshold) return RaceStartOutcome.None;

            switch (pressTiming)
            {
                case PressTiming.TooEarly: return RaceStartOutcome.Skid;
                case PressTiming.Perfect: return RaceStartOutcome.Turbo;
                case PressTiming.Late: return RaceStartOutcome.Boost;
                default: return RaceStartOutcome.None;
            }
        }
    }
}

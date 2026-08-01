namespace GrannyRacer.Walker
{
    public enum SlipperHeatStage
    {
        Safe,
        Warning,
        Critical,
        BurnedOut
    }

    public sealed class SlipperHeatModel
    {
        private float coolingDelayRemaining;

        public float Heat { get; private set; }
        public float ReplacementRemaining { get; private set; }
        public bool IsBurnedOut => ReplacementRemaining > 0f;

        public SlipperHeatStage GetStage(float warningThreshold, float criticalThreshold)
        {
            if (IsBurnedOut) return SlipperHeatStage.BurnedOut;
            if (Heat >= criticalThreshold) return SlipperHeatStage.Critical;
            return Heat >= warningThreshold ? SlipperHeatStage.Warning : SlipperHeatStage.Safe;
        }

        public void Tick(
            float deltaTime,
            bool boosting,
            bool replacementTap,
            float heatPerSecond,
            float coolingDelay,
            float coolingPerSecond,
            float replacementDuration,
            float replacementTapReduction,
            float postReplacementHeat)
        {
            if (deltaTime <= 0f) return;

            if (IsBurnedOut)
            {
                ReplacementRemaining -= deltaTime;
                if (replacementTap)
                {
                    ReplacementRemaining -= replacementTapReduction;
                }

                if (ReplacementRemaining <= 0f)
                {
                    ReplacementRemaining = 0f;
                    Heat = Clamp01(postReplacementHeat);
                    coolingDelayRemaining = coolingDelay;
                }

                return;
            }

            if (boosting)
            {
                Heat = Clamp01(Heat + heatPerSecond * deltaTime);
                coolingDelayRemaining = coolingDelay;
                if (Heat >= 1f)
                {
                    Heat = 1f;
                    ReplacementRemaining = replacementDuration;
                }
            }
            else if (coolingDelayRemaining > 0f)
            {
                coolingDelayRemaining -= deltaTime;
            }
            else
            {
                Heat = Clamp01(Heat - coolingPerSecond * deltaTime);
            }
        }

        public void AddCollisionHeat(float amount, float replacementDuration)
        {
            if (IsBurnedOut || amount <= 0f) return;
            Heat = Clamp01(Heat + amount);
            if (Heat >= 1f)
            {
                ReplacementRemaining = replacementDuration;
            }
        }

        public void Reset()
        {
            Heat = 0f;
            ReplacementRemaining = 0f;
            coolingDelayRemaining = 0f;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}

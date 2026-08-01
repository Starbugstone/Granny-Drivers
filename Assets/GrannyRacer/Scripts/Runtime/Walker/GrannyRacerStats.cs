using System;

namespace GrannyRacer.Walker
{
    public readonly struct GrannyStatMultipliers
    {
        public GrannyStatMultipliers(float acceleration, float adherence, float maximumSpeed)
        {
            Acceleration = Math.Max(0f, acceleration);
            Adherence = Math.Max(0f, adherence);
            MaximumSpeed = Math.Max(0f, maximumSpeed);
        }

        public float Acceleration { get; }
        public float Adherence { get; }
        public float MaximumSpeed { get; }

        public static GrannyStatMultipliers Balanced => new GrannyStatMultipliers(1f, 1f, 1f);
    }

    /// <summary>Runtime values after a selected granny is layered over the base walker.</summary>
    public readonly struct GrannyRacerStats
    {
        public GrannyRacerStats(float acceleration, float reverseAcceleration,
            float boostAcceleration, float maximumSpeed, float maximumReverseSpeed,
            float boostMaximumSpeed, float lateralGrip)
        {
            Acceleration = acceleration;
            ReverseAcceleration = reverseAcceleration;
            BoostAcceleration = boostAcceleration;
            MaximumSpeed = maximumSpeed;
            MaximumReverseSpeed = maximumReverseSpeed;
            BoostMaximumSpeed = boostMaximumSpeed;
            LateralGrip = lateralGrip;
        }

        public float Acceleration { get; }
        public float ReverseAcceleration { get; }
        public float BoostAcceleration { get; }
        public float MaximumSpeed { get; }
        public float MaximumReverseSpeed { get; }
        public float BoostMaximumSpeed { get; }
        public float LateralGrip { get; }

        public static GrannyRacerStats Resolve(in GrannyRacerStats baseStats,
            in GrannyStatMultipliers multipliers)
        {
            return new GrannyRacerStats(
                baseStats.Acceleration * multipliers.Acceleration,
                baseStats.ReverseAcceleration * multipliers.Acceleration,
                baseStats.BoostAcceleration * multipliers.Acceleration,
                baseStats.MaximumSpeed * multipliers.MaximumSpeed,
                baseStats.MaximumReverseSpeed * multipliers.MaximumSpeed,
                baseStats.BoostMaximumSpeed * multipliers.MaximumSpeed,
                baseStats.LateralGrip * multipliers.Adherence);
        }
    }
}

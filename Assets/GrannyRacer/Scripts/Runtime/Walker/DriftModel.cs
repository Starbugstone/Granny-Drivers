namespace GrannyRacer.Walker
{
    /// <summary>
    /// Charge tiers reached while drifting. Each tier releases a stronger exit boost, and the
    /// slipper smoke is tinted to match so the player can read the charge without a HUD.
    /// </summary>
    public enum DriftChargeStage
    {
        None,
        Red,
        Yellow,
        Blue
    }

    /// <summary>
    /// Tuning for <see cref="DriftModel"/>. Kept as a plain struct so the drift rules can be
    /// tested without a scene or a ScriptableObject; <see cref="WalkerHandlingSettings"/>
    /// builds one per physics step.
    /// </summary>
    public readonly struct DriftTuning
    {
        public DriftTuning(
            float minimumSpeed,
            float minimumSteer,
            float insideChargeRate,
            float outsideChargeRate,
            float redSeconds,
            float yellowSeconds,
            float blueSeconds,
            float redBoostSpeed,
            float yellowBoostSpeed,
            float blueBoostSpeed,
            float boostDuration)
        {
            MinimumSpeed = minimumSpeed;
            MinimumSteer = minimumSteer;
            InsideChargeRate = insideChargeRate;
            OutsideChargeRate = outsideChargeRate;
            RedSeconds = redSeconds;
            YellowSeconds = yellowSeconds;
            BlueSeconds = blueSeconds;
            RedBoostSpeed = redBoostSpeed;
            YellowBoostSpeed = yellowBoostSpeed;
            BlueBoostSpeed = blueBoostSpeed;
            BoostDuration = boostDuration;
        }

        public float MinimumSpeed { get; }
        public float MinimumSteer { get; }
        public float InsideChargeRate { get; }
        public float OutsideChargeRate { get; }
        public float RedSeconds { get; }
        public float YellowSeconds { get; }
        public float BlueSeconds { get; }
        public float RedBoostSpeed { get; }
        public float YellowBoostSpeed { get; }
        public float BlueBoostSpeed { get; }
        public float BoostDuration { get; }
    }

    /// <summary>
    /// Mario-Kart style charged drift. The hop commits the walker to a drift direction, holding
    /// the jump button keeps it charging, and letting go cashes the charge in as a speed boost.
    /// </summary>
    public sealed class DriftModel
    {
        private float pendingBoostSpeed;

        public bool IsDrifting { get; private set; }

        /// <summary>-1 when drifting left, +1 when drifting right. Fixed at the hop.</summary>
        public int Direction { get; private set; }

        /// <summary>Charge in seconds. Steering into the drift banks it faster than steering out.</summary>
        public float Charge { get; private set; }

        public float BoostRemaining { get; private set; }
        public DriftChargeStage BoostStage { get; private set; }

        public static DriftChargeStage StageFor(float charge, float redSeconds,
            float yellowSeconds, float blueSeconds)
        {
            if (charge >= blueSeconds) return DriftChargeStage.Blue;
            if (charge >= yellowSeconds) return DriftChargeStage.Yellow;
            return charge >= redSeconds ? DriftChargeStage.Red : DriftChargeStage.None;
        }

        public DriftChargeStage GetStage(in DriftTuning tuning)
        {
            return IsDrifting
                ? StageFor(Charge, tuning.RedSeconds, tuning.YellowSeconds, tuning.BlueSeconds)
                : DriftChargeStage.None;
        }

        /// <summary>
        /// Called on the hop. Fails quietly when the walker is too slow or is not steering,
        /// so a plain jump stays a plain jump.
        /// </summary>
        public bool TryStart(in DriftTuning tuning, float speed, float steer)
        {
            if (IsDrifting || speed < tuning.MinimumSpeed || Abs(steer) < tuning.MinimumSteer)
            {
                return false;
            }

            IsDrifting = true;
            Direction = steer < 0f ? -1 : 1;
            Charge = 0f;
            return true;
        }

        /// <summary>
        /// Advances the drift and the exit boost. Returns whether the walker is still drifting.
        /// Airborne time keeps the drift alive but does not bank charge — only slipper on
        /// tarmac counts.
        /// </summary>
        public bool Tick(in DriftTuning tuning, float deltaTime, bool held, bool grounded,
            float speed, float steer)
        {
            if (deltaTime <= 0f) return IsDrifting;

            if (BoostRemaining > 0f)
            {
                BoostRemaining -= deltaTime;
                if (BoostRemaining <= 0f)
                {
                    BoostRemaining = 0f;
                    BoostStage = DriftChargeStage.None;
                }
            }

            if (!IsDrifting) return false;

            if (!held || speed < tuning.MinimumSpeed)
            {
                End(tuning);
                return false;
            }

            if (!grounded) return true;

            var into = steer * Direction;
            var rate = into > 0.1f
                ? tuning.InsideChargeRate
                : (into < -0.1f ? tuning.OutsideChargeRate : 1f);
            Charge += rate * deltaTime;
            return true;
        }

        /// <summary>Ends the drift and arms the exit boost for the tier that was reached.</summary>
        public void End(in DriftTuning tuning)
        {
            if (!IsDrifting) return;

            var stage = StageFor(Charge, tuning.RedSeconds, tuning.YellowSeconds, tuning.BlueSeconds);
            IsDrifting = false;
            Charge = 0f;

            if (stage == DriftChargeStage.None) return;

            BoostStage = stage;
            BoostRemaining = tuning.BoostDuration;
            pendingBoostSpeed = stage switch
            {
                DriftChargeStage.Blue => tuning.BlueBoostSpeed,
                DriftChargeStage.Yellow => tuning.YellowBoostSpeed,
                _ => tuning.RedBoostSpeed
            };
        }

        /// <summary>
        /// Returns the one-shot exit impulse in m/s, then clears it. The controller applies it
        /// as a velocity change so the kick is felt on the frame the button is released.
        /// </summary>
        public float ConsumeBoostImpulse()
        {
            var impulse = pendingBoostSpeed;
            pendingBoostSpeed = 0f;
            return impulse;
        }

        public void Reset()
        {
            IsDrifting = false;
            Direction = 0;
            Charge = 0f;
            BoostRemaining = 0f;
            BoostStage = DriftChargeStage.None;
            pendingBoostSpeed = 0f;
        }

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }
    }
}

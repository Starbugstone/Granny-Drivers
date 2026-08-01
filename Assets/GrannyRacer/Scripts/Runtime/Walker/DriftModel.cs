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
            float minimumAirTime,
            float engageWindow,
            float engageRamp,
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
            MinimumAirTime = minimumAirTime;
            EngageWindow = engageWindow;
            EngageRamp = engageRamp;
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

        /// <summary>
        /// How long the walker must be off the ground before the hop counts as a real hop.
        /// The ground probe reaches further than the first few centimetres of the jump, so
        /// without this a hop would read as "landed" one physics step after take-off.
        /// </summary>
        public float MinimumAirTime { get; }

        /// <summary>Grace period after landing in which steering still engages the drift.</summary>
        public float EngageWindow { get; }

        /// <summary>Seconds the drift takes to blend from raw steering onto its own line.</summary>
        public float EngageRamp { get; }

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
    /// Mario-Kart style charged drift, in three phases.
    ///
    /// The hop only <em>arms</em> the drift. Nothing slides, smokes or marks the road while
    /// Granny is in the air — the drift engages on the landing, in whichever direction she is
    /// steering when the slippers touch down, and a short grace window after touchdown lets a
    /// player who steers slightly late still get it. Holding the button keeps it charging;
    /// letting go cashes the charge in as a speed boost.
    /// </summary>
    public sealed class DriftModel
    {
        private float pendingBoostSpeed;
        private float airborneSeconds;
        private float engageRemaining;
        private bool landedFromHop;

        /// <summary>The hop is in the air or waiting out its landing window. Not yet a drift.</summary>
        public bool IsArmed { get; private set; }

        public bool IsDrifting { get; private set; }

        /// <summary>-1 when drifting left, +1 when drifting right. Fixed at the landing.</summary>
        public int Direction { get; private set; }

        /// <summary>Charge in seconds. Steering into the drift banks it faster than steering out.</summary>
        public float Charge { get; private set; }

        /// <summary>
        /// 0 on the frame the drift engages, 1 once it holds its own line. The controller
        /// blends steering with it so a landing does not snap the walker sideways.
        /// </summary>
        public float EngageWeight { get; private set; }

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
        /// Called on the hop. Arms the drift without committing to a direction; the direction is
        /// read at the landing. Fails quietly below the minimum speed, so a slow hop stays a
        /// plain hop.
        /// </summary>
        public bool TryArm(in DriftTuning tuning, float speed)
        {
            if (IsDrifting || IsArmed || speed < tuning.MinimumSpeed)
            {
                return false;
            }

            IsArmed = true;
            airborneSeconds = 0f;
            engageRemaining = 0f;
            landedFromHop = false;
            return true;
        }

        /// <summary>
        /// Advances the armed hop, the drift and the exit boost. Returns whether the walker is
        /// drifting. Airborne time keeps an engaged drift alive but does not bank charge — only
        /// slipper on tarmac counts.
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

            if (IsArmed)
            {
                TickArmed(tuning, deltaTime, held, grounded, speed, steer);
            }

            if (!IsDrifting) return false;

            if (!held || speed < tuning.MinimumSpeed)
            {
                End(tuning);
                return false;
            }

            EngageWeight = tuning.EngageRamp > 0f
                ? Min(1f, EngageWeight + deltaTime / tuning.EngageRamp)
                : 1f;

            if (!grounded) return true;

            var into = steer * Direction;
            var rate = into > 0.1f
                ? tuning.InsideChargeRate
                : (into < -0.1f ? tuning.OutsideChargeRate : 1f);
            Charge += rate * deltaTime;
            return true;
        }

        /// <summary>
        /// Runs the armed hop. Nothing here starts a slide: it waits for real air time, then for
        /// the touchdown, and only then reads the stick to pick a drift direction.
        /// </summary>
        private void TickArmed(in DriftTuning tuning, float deltaTime, bool held, bool grounded,
            float speed, float steer)
        {
            if (!held || speed < tuning.MinimumSpeed)
            {
                IsArmed = false;
                return;
            }

            if (!grounded)
            {
                airborneSeconds += deltaTime;
                if (airborneSeconds >= tuning.MinimumAirTime)
                {
                    landedFromHop = false;
                    engageRemaining = tuning.EngageWindow;
                }

                return;
            }

            // Contact before the minimum air time is the tail of the take-off, not a landing:
            // the ground probe still reports the road for the first few centimetres of the hop.
            if (airborneSeconds < tuning.MinimumAirTime)
            {
                airborneSeconds = 0f;
                return;
            }

            landedFromHop = true;
            if (Abs(steer) >= tuning.MinimumSteer)
            {
                Engage(steer);
                return;
            }

            engageRemaining -= deltaTime;
            if (engageRemaining <= 0f) IsArmed = false;
        }

        private void Engage(float steer)
        {
            IsArmed = false;
            IsDrifting = true;
            Direction = steer < 0f ? -1 : 1;
            Charge = 0f;
            EngageWeight = 0f;
        }

        /// <summary>Ends the drift and arms the exit boost for the tier that was reached.</summary>
        public void End(in DriftTuning tuning)
        {
            if (!IsDrifting) return;

            var stage = StageFor(Charge, tuning.RedSeconds, tuning.YellowSeconds, tuning.BlueSeconds);
            IsDrifting = false;
            Charge = 0f;
            EngageWeight = 0f;

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
            IsArmed = false;
            IsDrifting = false;
            Direction = 0;
            Charge = 0f;
            EngageWeight = 0f;
            BoostRemaining = 0f;
            BoostStage = DriftChargeStage.None;
            pendingBoostSpeed = 0f;
            airborneSeconds = 0f;
            engageRemaining = 0f;
            landedFromHop = false;
        }

        /// <summary>True once an armed hop has touched down and is waiting on the stick.</summary>
        public bool IsWaitingToEngage => IsArmed && landedFromHop;

        private static float Abs(float value)
        {
            return value < 0f ? -value : value;
        }

        private static float Min(float a, float b)
        {
            return a < b ? a : b;
        }
    }
}

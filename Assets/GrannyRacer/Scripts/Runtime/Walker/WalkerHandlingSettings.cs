using UnityEngine;
using UnityEngine.Serialization;

namespace GrannyRacer.Walker
{
    [CreateAssetMenu(menuName = "Granny Racer/Walker Handling", fileName = "WalkerHandling_POC")]
    public sealed class WalkerHandlingSettings : ScriptableObject
    {
        [Header("Drive")]
        [Min(0f)] public float acceleration = 24f;
        [Min(0f)] public float reverseAcceleration = 12f;
        [Min(0f)] public float braking = 32f;
        [Min(0f)] public float maximumSpeed = 15f;
        [Min(0f)] public float maximumReverseSpeed = 5f;
        [Min(0f)] public float boostAcceleration = 38f;
        [Min(0f)] public float boostMaximumSpeed = 22f;
        [Range(0.1f, 1f)] public float boostGripScale = 0.72f;

        [Header("Steering")]
        [Min(0f)] public float steeringDegreesPerSecond = 125f;
        [Min(0.01f)] public float steeringFalloffSpeed = 15f;
        [Range(0.1f, 1f)] public float topSpeedSteeringScale = 0.42f;
        [Min(0f)] public float lateralGrip = 7f;

        [Tooltip("Coast-down deceleration. The walker collider is frictionless so that PhysX "
            + "friction cannot fight the drive force, so rolling resistance is modelled here.")]
        [Min(0f)] public float rollingResistance = 5f;

        [Header("Grounding")]
        [Min(0.01f)] public float groundProbeRadius = 0.42f;
        [Min(0.01f)] public float groundProbeDistance = 0.8f;
        [Min(0f)] public float downforce = 18f;
        [Min(0f)] public float uprightStrength = 18f;
        [Min(0f)] public float uprightDamping = 5f;

        [Header("Jump")]
        [Min(0f)] public float jumpVelocityChange = 5.2f;
        [Min(0f)] public float jumpInputBuffer = 0.12f;

        [Header("Skid")]
        [Min(0f)] public float skidMinimumSpeed = 7f;
        [Range(0f, 1f)] public float skidMinimumBrake = 0.35f;
        [Range(0f, 1f)] public float skidMinimumSteer = 0.35f;
        [Range(0.05f, 1f)] public float skidGripScale = 0.38f;

        [Header("Drift")]
        [Tooltip("Hop with Jump above this speed to arm a drift. The drift itself engages on "
            + "the landing, in whichever direction the player is steering then.")]
        [Min(0f)] public float driftMinimumSpeed = 5f;
        [Range(0f, 1f)] public float driftMinimumSteer = 0.3f;

        [Tooltip("Air time the hop must clear before a ground contact counts as the landing. "
            + "The ground probe outreaches the first centimetres of the hop, so without this "
            + "the drift would engage a physics step after take-off.")]
        [Min(0f)] public float driftMinimumAirTime = 0.08f;

        [Tooltip("Grace period after landing in which steering still engages the drift, so a "
            + "player who turns in slightly late is not punished.")]
        [Min(0f)] public float driftEngageWindow = 0.6f;

        [Tooltip("Seconds the drift takes to blend from raw steering onto its own line. Stops "
            + "the landing from snapping the walker sideways.")]
        [Min(0f)] public float driftEngageRamp = 0.15f;

        [Tooltip("Steering the drift applies on its own, before the player's input is added.")]
        [Range(0f, 1f)] public float driftSteerBias = 0.7f;

        [Tooltip("Extra lock available by steering into the drift, on top of the bias.")]
        [FormerlySerializedAs("driftSteerControl")]
        [Range(0f, 1f)] public float driftInwardSteerControl = 0.38f;

        [Tooltip("How much counter-steering opens the drift back out. Set high enough that a "
            + "full counter runs nearly straight while still holding the drift.")]
        [Range(0f, 1f)] public float driftCounterSteerControl = 0.62f;

        [Tooltip("Lock a fully countered drift still holds. Never zero — a drift that resolves "
            + "to neutral stops reading as a drift.")]
        [Range(0f, 0.5f)] public float driftMinimumHold = 0.08f;

        [Tooltip("Yaw rate multiplier while drifting. Above 1 so a drifted corner genuinely "
            + "turns tighter than a gripped one.")]
        [Range(0.5f, 2f)] public float driftSteeringScale = 1.25f;

        [Range(0.05f, 1f)] public float driftGripScale = 0.3f;
        [Tooltip("Charge multiplier while steering into the drift.")]
        [Min(0f)] public float driftInsideChargeRate = 1.35f;
        [Tooltip("Charge multiplier while counter-steering out of the drift.")]
        [Min(0f)] public float driftOutsideChargeRate = 0.55f;
        [Min(0f)] public float driftRedSeconds = 0.7f;
        [Min(0f)] public float driftYellowSeconds = 1.6f;
        [Min(0f)] public float driftBlueSeconds = 2.6f;
        [Min(0f)] public float driftRedBoostSpeed = 2.2f;
        [Min(0f)] public float driftYellowBoostSpeed = 3.8f;
        [Min(0f)] public float driftBlueBoostSpeed = 5.6f;
        [Tooltip("How long the exit boost keeps the raised speed cap.")]
        [Min(0f)] public float driftBoostDuration = 0.9f;

        [Header("Presentation")]
        [Min(0f)] public float visualLeanDegrees = 11f;
        [Min(0f)] public float visualLeanSpeed = 8f;
        [Min(0f)] public float wobbleDegrees = 8f;
        [Min(0f)] public float wobbleDuration = 0.8f;
        [Min(0f)] public float collisionHeatVelocity = 7f;

        public DriftTuning CreateDriftTuning()
        {
            return new DriftTuning(driftMinimumSpeed, driftMinimumSteer, driftMinimumAirTime,
                driftEngageWindow, driftEngageRamp, driftInsideChargeRate,
                driftOutsideChargeRate, driftRedSeconds, driftYellowSeconds, driftBlueSeconds,
                driftRedBoostSpeed, driftYellowBoostSpeed, driftBlueBoostSpeed, driftBoostDuration);
        }
    }
}

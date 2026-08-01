using UnityEngine;

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
        [Tooltip("Hop with Jump above this speed while steering to commit to a charged drift.")]
        [Min(0f)] public float driftMinimumSpeed = 5f;
        [Range(0f, 1f)] public float driftMinimumSteer = 0.3f;
        [Tooltip("Steering the drift applies on its own, before the player's input is added.")]
        [Range(0f, 1f)] public float driftSteerBias = 0.7f;
        [Tooltip("How much the player can tighten or open the drift line.")]
        [Range(0f, 1f)] public float driftSteerControl = 0.45f;
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
            return new DriftTuning(driftMinimumSpeed, driftMinimumSteer, driftInsideChargeRate,
                driftOutsideChargeRate, driftRedSeconds, driftYellowSeconds, driftBlueSeconds,
                driftRedBoostSpeed, driftYellowBoostSpeed, driftBlueBoostSpeed, driftBoostDuration);
        }
    }
}

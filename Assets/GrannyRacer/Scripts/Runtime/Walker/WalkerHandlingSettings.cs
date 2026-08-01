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

        [Header("Presentation")]
        [Min(0f)] public float visualLeanDegrees = 11f;
        [Min(0f)] public float visualLeanSpeed = 8f;
        [Min(0f)] public float wobbleDegrees = 8f;
        [Min(0f)] public float wobbleDuration = 0.8f;
        [Min(0f)] public float collisionHeatVelocity = 7f;
    }
}

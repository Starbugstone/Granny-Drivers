using GrannyRacer.Input;
using UnityEngine;

namespace GrannyRacer.Walker
{
    [RequireComponent(typeof(Rigidbody), typeof(PlayerRacerInput))]
    [DefaultExecutionOrder(-1000)]
    public sealed class ArcadeWalkerController : MonoBehaviour
    {
        [SerializeField] private WalkerHandlingSettings handling;
        [SerializeField] private SlipperHeatSettings heatSettings;
        [SerializeField] private Transform visualRoot;

        private Rigidbody body;
        private IRacerInputSource inputSource;
        private RacerInputState input;
        private Quaternion spawnRotation;
        private Vector3 spawnPosition;
        private Quaternion visualBaseRotation;
        private bool grounded;
        private bool canDrive = true;
        private float wobbleRemaining;
        private readonly SlipperHeatModel heat = new SlipperHeatModel();

        public float Speed => body == null ? 0f : body.linearVelocity.magnitude;
        public bool IsGrounded => grounded;
        public WalkerHandlingSettings Handling => handling;
        public float Heat => heat.Heat;
        public float ReplacementRemaining => heat.ReplacementRemaining;
        public bool IsBoosting { get; private set; }
        public SlipperHeatStage HeatStage => heatSettings == null
            ? SlipperHeatStage.Safe
            : heat.GetStage(heatSettings.warningThreshold, heatSettings.criticalThreshold);

        public void Configure(WalkerHandlingSettings settings, SlipperHeatSettings slipperHeat, Transform visuals)
        {
            handling = settings;
            heatSettings = slipperHeat;
            visualRoot = visuals;
        }

        public void SetCanDrive(bool value)
        {
            canDrive = value;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            inputSource = GetComponent<PlayerRacerInput>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            visualBaseRotation = visualRoot == null ? Quaternion.identity : visualRoot.localRotation;
        }

        private void Update()
        {
            input = inputSource.Sample();
            if (input.Reset)
            {
                ResetToSpawn();
            }

            if (heatSettings != null)
            {
                IsBoosting = canDrive && input.Boost && !heat.IsBurnedOut;
                heat.Tick(Time.deltaTime, IsBoosting, input.BoostPressed, heatSettings.heatPerSecond,
                    heatSettings.coolingDelay, heatSettings.coolingPerSecond,
                    heatSettings.replacementDuration, heatSettings.replacementTapReduction,
                    heatSettings.postReplacementHeat);
            }

            if (visualRoot != null && handling != null)
            {
                if (wobbleRemaining > 0f) wobbleRemaining -= Time.deltaTime;
                var wobble = wobbleRemaining > 0f
                    ? Mathf.Sin(Time.time * 24f) * handling.wobbleDegrees
                        * (wobbleRemaining / Mathf.Max(0.01f, handling.wobbleDuration))
                    : 0f;
                var slipperShuffle = heat.IsBurnedOut ? Mathf.Sin(Time.time * 31f) * 7f : 0f;
                var target = visualBaseRotation * Quaternion.Euler(0f, 0f,
                    -input.Steer * handling.visualLeanDegrees + wobble)
                    * Quaternion.Euler(slipperShuffle, 0f, 0f);
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, target, handling.visualLeanSpeed * Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            if (handling == null)
            {
                return;
            }

            grounded = Physics.SphereCast(transform.position + transform.up * 0.1f, handling.groundProbeRadius,
                -transform.up, out _, handling.groundProbeDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            if (!grounded)
            {
                return;
            }

            var velocity = body.linearVelocity;
            var forwardSpeed = Vector3.Dot(velocity, transform.forward);
            var lateralSpeed = Vector3.Dot(velocity, transform.right);
            var driveInput = canDrive ? WalkerHandlingMath.DriveInput(input.Throttle, input.Brake, forwardSpeed) : 0f;

            var acceleration = driveInput >= 0f
                ? (IsBoosting ? handling.boostAcceleration : handling.acceleration)
                : handling.reverseAcceleration;
            var speedLimit = driveInput >= 0f
                ? (IsBoosting ? handling.boostMaximumSpeed : handling.maximumSpeed)
                : handling.maximumReverseSpeed;
            if (heat.IsBurnedOut && heatSettings != null)
            {
                speedLimit *= heatSettings.burnoutSpeedScale;
            }
            if (Mathf.Abs(forwardSpeed) < speedLimit || Mathf.Sign(driveInput) != Mathf.Sign(forwardSpeed))
            {
                body.AddForce(transform.forward * (driveInput * acceleration), ForceMode.Acceleration);
            }

            if (input.Brake > 0f && forwardSpeed > 0.5f)
            {
                body.AddForce(-transform.forward * (input.Brake * handling.braking), ForceMode.Acceleration);
            }
            else if (Mathf.Approximately(driveInput, 0f) && Mathf.Abs(forwardSpeed) > 0.1f)
            {
                // The collider is frictionless so PhysX cannot cancel the drive force, so the
                // coast-down has to be applied explicitly.
                var resistance = Mathf.Min(handling.rollingResistance,
                    Mathf.Abs(forwardSpeed) / Time.fixedDeltaTime);
                body.AddForce(-transform.forward * (Mathf.Sign(forwardSpeed) * resistance),
                    ForceMode.Acceleration);
            }

            var grip = IsBoosting ? handling.lateralGrip * handling.boostGripScale : handling.lateralGrip;
            body.AddForce(-transform.right * (lateralSpeed * grip), ForceMode.Acceleration);
            body.AddForce(-transform.up * handling.downforce, ForceMode.Acceleration);

            var steeringScale = WalkerHandlingMath.SteeringScale(Mathf.Abs(forwardSpeed),
                handling.steeringFalloffSpeed, handling.topSpeedSteeringScale);
            var direction = forwardSpeed < -0.2f ? -1f : 1f;
            var yaw = canDrive
                ? input.Steer * direction * handling.steeringDegreesPerSecond * steeringScale * Time.fixedDeltaTime
                : 0f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));

            var uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            body.AddTorque(uprightAxis * handling.uprightStrength - body.angularVelocity * handling.uprightDamping,
                ForceMode.Acceleration);
        }

        public void ResetToSpawn()
        {
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        public void SetResetPose(Vector3 position, Quaternion rotation)
        {
            spawnPosition = position;
            spawnRotation = rotation;
        }

        public void ResetForRace(Vector3 position, Quaternion rotation)
        {
            SetResetPose(position, rotation);
            heat.Reset();
            ResetToSpawn();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (handling == null || heatSettings == null) return;
            var impact = collision.relativeVelocity.magnitude;
            if (impact < handling.collisionHeatVelocity) return;
            heat.AddCollisionHeat(heatSettings.collisionHeat, heatSettings.replacementDuration);
            wobbleRemaining = handling.wobbleDuration;
        }
    }
}

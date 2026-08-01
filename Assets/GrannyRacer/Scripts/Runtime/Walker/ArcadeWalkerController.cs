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
        private float jumpRequestRemaining;
        private float jumpPresentationRemaining;
        private float wobbleRemaining;
        private readonly SlipperHeatModel heat = new SlipperHeatModel();
        private readonly DriftModel drift = new DriftModel();

        public float Speed => body == null ? 0f : body.linearVelocity.magnitude;
        public bool IsGrounded => grounded;
        public bool IsJumping => !grounded || jumpPresentationRemaining > 0f;
        public bool IsSkidding { get; private set; }
        public float VerticalSpeed => body == null ? 0f : Vector3.Dot(body.linearVelocity, transform.up);
        public WalkerHandlingSettings Handling => handling;
        public float Heat => heat.Heat;
        public float ReplacementRemaining => heat.ReplacementRemaining;
        public bool IsBoosting { get; private set; }
        public bool BoostPressedThisFrame { get; private set; }
        public float SteeringInput { get; private set; }
        public SlipperHeatStage HeatStage => heatSettings == null
            ? SlipperHeatStage.Safe
            : heat.GetStage(heatSettings.warningThreshold, heatSettings.criticalThreshold);

        public bool IsDrifting => drift.IsDrifting;
        public int DriftDirection => drift.Direction;
        public float DriftCharge => drift.Charge;
        public DriftChargeStage DriftStage => handling == null
            ? DriftChargeStage.None
            : drift.GetStage(handling.CreateDriftTuning());

        /// <summary>The tier the current exit boost was released at, for VFX and HUD.</summary>
        public DriftChargeStage DriftBoostStage => drift.BoostStage;
        public bool DriftBoostActive => drift.BoostRemaining > 0f;

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
            BoostPressedThisFrame = canDrive && input.BoostPressed;
            SteeringInput = input.Steer;
            if (canDrive && input.JumpPressed && handling != null)
            {
                jumpRequestRemaining = handling.jumpInputBuffer;
            }

            if (jumpPresentationRemaining > 0f)
            {
                jumpPresentationRemaining -= Time.deltaTime;
            }
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

            if (jumpRequestRemaining > 0f)
            {
                jumpRequestRemaining -= Time.fixedDeltaTime;
            }

            grounded = Physics.SphereCast(transform.position + transform.up * 0.1f, handling.groundProbeRadius,
                -transform.up, out _, handling.groundProbeDistance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            var velocity = body.linearVelocity;
            var forwardSpeed = Vector3.Dot(velocity, transform.forward);
            var lateralSpeed = Vector3.Dot(velocity, transform.right);

            if (grounded && canDrive && jumpRequestRemaining > 0f)
            {
                body.AddForce(transform.up * handling.jumpVelocityChange, ForceMode.VelocityChange);
                jumpRequestRemaining = 0f;
                jumpPresentationRemaining = 0.12f;
                grounded = false;
                // The hop is the drift entry. Hopping while steering at speed commits the
                // walker to a drift; hopping straight stays an ordinary jump.
                drift.TryStart(handling.CreateDriftTuning(), Mathf.Abs(forwardSpeed), input.Steer);
            }

            TickDrift(Mathf.Abs(forwardSpeed));

            if (!grounded)
            {
                // The drift survives a hop or a bump, so the pose holds while airborne.
                IsSkidding = drift.IsDrifting;
                return;
            }

            var brakeSlide = canDrive && WalkerHandlingMath.ShouldSkid(forwardSpeed, input.Brake,
                input.Steer, handling.skidMinimumSpeed, handling.skidMinimumBrake,
                handling.skidMinimumSteer);
            IsSkidding = brakeSlide || drift.IsDrifting;
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
            if (drift.BoostRemaining > 0f && driveInput >= 0f)
            {
                // The exit boost is worth nothing if the normal cap claws the speed straight
                // back, so it raises the ceiling for as long as it lasts.
                speedLimit = Mathf.Max(speedLimit, handling.boostMaximumSpeed);
            }
            if (Mathf.Abs(forwardSpeed) < speedLimit || Mathf.Sign(driveInput) != Mathf.Sign(forwardSpeed))
            {
                body.AddForce(transform.forward * (driveInput * acceleration), ForceMode.Acceleration);
            }

            if (input.Brake > 0f && forwardSpeed > 0.5f)
            {
                body.AddForce(-transform.forward * (input.Brake * handling.braking), ForceMode.Acceleration);
            }
            else if (Mathf.Approximately(driveInput, 0f) && Mathf.Abs(forwardSpeed) > 0.1f
                && drift.BoostRemaining <= 0f)
            {
                // The collider is frictionless so PhysX cannot cancel the drive force, so the
                // coast-down has to be applied explicitly.
                var resistance = Mathf.Min(handling.rollingResistance,
                    Mathf.Abs(forwardSpeed) / Time.fixedDeltaTime);
                body.AddForce(-transform.forward * (Mathf.Sign(forwardSpeed) * resistance),
                    ForceMode.Acceleration);
            }

            var grip = IsBoosting ? handling.lateralGrip * handling.boostGripScale : handling.lateralGrip;
            if (drift.IsDrifting) grip *= handling.driftGripScale;
            else if (brakeSlide) grip *= handling.skidGripScale;
            body.AddForce(-transform.right * (lateralSpeed * grip), ForceMode.Acceleration);
            body.AddForce(-transform.up * handling.downforce, ForceMode.Acceleration);

            var steeringScale = WalkerHandlingMath.SteeringScale(Mathf.Abs(forwardSpeed),
                handling.steeringFalloffSpeed, handling.topSpeedSteeringScale);
            var direction = forwardSpeed < -0.2f ? -1f : 1f;
            var steer = drift.IsDrifting
                ? WalkerHandlingMath.DriftSteer(drift.Direction, input.Steer,
                    handling.driftSteerBias, handling.driftSteerControl)
                : input.Steer;
            var yaw = canDrive
                ? steer * direction * handling.steeringDegreesPerSecond * steeringScale * Time.fixedDeltaTime
                : 0f;
            body.MoveRotation(body.rotation * Quaternion.Euler(0f, yaw, 0f));

            var uprightAxis = Vector3.Cross(transform.up, Vector3.up);
            body.AddTorque(uprightAxis * handling.uprightStrength - body.angularVelocity * handling.uprightDamping,
                ForceMode.Acceleration);
        }

        private void TickDrift(float speed)
        {
            var tuning = handling.CreateDriftTuning();
            drift.Tick(tuning, Time.fixedDeltaTime, canDrive && input.JumpHeld, grounded, speed,
                input.Steer);

            var impulse = drift.ConsumeBoostImpulse();
            if (impulse > 0f)
            {
                body.AddForce(transform.forward * impulse, ForceMode.VelocityChange);
            }
        }

        public void ResetToSpawn()
        {
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            drift.Reset();
            IsSkidding = false;
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
            drift.Reset();
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

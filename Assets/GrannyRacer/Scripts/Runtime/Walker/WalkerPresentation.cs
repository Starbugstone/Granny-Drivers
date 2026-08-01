using GrannyRacer.Audio;
using GrannyRacer.Characters;
using UnityEngine;

namespace GrannyRacer.Walker
{
    [DisallowMultipleComponent]
    public sealed class WalkerPresentation : MonoBehaviour
    {
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int DriveState = Animator.StringToHash("Base Layer.Drive");
        private static readonly int TurnLeftState = Animator.StringToHash("Base Layer.TurnLeft");
        private static readonly int TurnRightState = Animator.StringToHash("Base Layer.TurnRight");
        private static readonly int BoostState = Animator.StringToHash("Base Layer.Boost");
        private static readonly int HitReactState = Animator.StringToHash("Base Layer.HitReact");

        [SerializeField] private ArcadeWalkerController controller;
        [SerializeField] private Animator animator;
        [SerializeField] private GrannyVoice voice;
        [SerializeField] private SlipperSwapper slipperSwapper;
        [Min(0f)] [SerializeField] private float movingSpeed = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float turningThreshold = 0.25f;
        [Min(0f)] [SerializeField] private float hitAnimationDuration = 0.75f;
        [Min(0f)] [SerializeField] private float minimumHitSpeed = 3f;
        [Min(0f)] [SerializeField] private float crossFadeDuration = 0.12f;
        [Min(0.01f)] [SerializeField] private float minimumDrivePlaybackSpeed = 0.8f;
        [Min(0.01f)] [SerializeField] private float maximumDrivePlaybackSpeed = 2.2f;
        [Range(0f, 45f)] [SerializeField] private float jumpTuckDegrees = 24f;
        [Range(0f, 45f)] [SerializeField] private float skidBraceDegrees = 18f;

        private int activeState;
        private int nextSlipperIndex;
        private float hitAnimationRemaining;
        private SlipperHeatStage previousHeatStage;
        private Transform hips;
        private Transform thighLeft;
        private Transform thighRight;
        private Transform upperArmLeft;
        private Transform upperArmRight;

        public void Configure(ArcadeWalkerController walker, Animator modelAnimator,
            GrannyVoice grannyVoice, SlipperSwapper swapper)
        {
            controller = walker;
            animator = modelAnimator;
            voice = grannyVoice;
            slipperSwapper = swapper;
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ArcadeWalkerController>();
            if (voice == null) voice = GetComponent<GrannyVoice>();
            if (animator != null)
            {
                hips = FindDescendant(animator.transform, "Hips");
                thighLeft = FindDescendant(animator.transform, "Thigh_L");
                thighRight = FindDescendant(animator.transform, "Thigh_R");
                upperArmLeft = FindDescendant(animator.transform, "UpperArm_L");
                upperArmRight = FindDescendant(animator.transform, "UpperArm_R");
            }
            previousHeatStage = controller == null ? SlipperHeatStage.Safe : controller.HeatStage;
        }

        private void LateUpdate()
        {
            if (controller == null || animator == null) return;

            if (controller.IsJumping)
            {
                var riseWeight = Mathf.InverseLerp(-4f, 4f, controller.VerticalSpeed);
                var tuck = Mathf.Lerp(jumpTuckDegrees * 0.55f, jumpTuckDegrees, riseWeight);
                AddLocalRotation(hips, new Vector3(-tuck * 0.3f, 0f, 0f));
                AddLocalRotation(thighLeft, new Vector3(-tuck, 0f, -tuck * 0.25f));
                AddLocalRotation(thighRight, new Vector3(-tuck, 0f, tuck * 0.25f));
                AddLocalRotation(upperArmLeft, new Vector3(tuck * 0.4f, 0f, -tuck * 0.35f));
                AddLocalRotation(upperArmRight, new Vector3(tuck * 0.4f, 0f, tuck * 0.35f));
                return;
            }

            if (!controller.IsSkidding) return;
            // A drift braces against its committed direction, not against whatever the player
            // is doing with the stick — counter-steering must not flip Granny's hips.
            var side = controller.IsDrifting
                ? controller.DriftDirection
                : Mathf.Sign(controller.SteeringInput);
            var chatter = Mathf.Sin(Time.time * 28f) * 2f;
            AddLocalRotation(hips, new Vector3(0f, -side * skidBraceDegrees, chatter));
            AddLocalRotation(thighLeft, new Vector3(0f, side * skidBraceDegrees * 0.4f,
                -skidBraceDegrees * 0.55f));
            AddLocalRotation(thighRight, new Vector3(0f, side * skidBraceDegrees * 0.4f,
                skidBraceDegrees * 0.55f));
            AddLocalRotation(upperArmLeft, new Vector3(0f, 0f, -skidBraceDegrees * 0.45f));
            AddLocalRotation(upperArmRight, new Vector3(0f, 0f, skidBraceDegrees * 0.45f));
        }

        private void Start()
        {
            PlayState(IdleState, 0f);
        }

        private void Update()
        {
            if (controller == null || animator == null) return;

            if (controller.BoostPressedThisFrame && voice != null)
            {
                voice.ReactToInput();
            }

            UpdateSlippers();

            if (hitAnimationRemaining > 0f)
            {
                hitAnimationRemaining -= Time.deltaTime;
                animator.speed = 1f;
                return;
            }

            var state = SelectLocomotionState();
            PlayState(state, crossFadeDuration);
            UpdatePlaybackSpeed(state);
        }

        private int SelectLocomotionState()
        {
            if (controller.IsBoosting) return BoostState;
            if (controller.Speed < movingSpeed)
            {
                if (controller.SteeringInput < -turningThreshold) return TurnLeftState;
                if (controller.SteeringInput > turningThreshold) return TurnRightState;
                return IdleState;
            }

            return DriveState;
        }

        private void UpdatePlaybackSpeed(int state)
        {
            if (state != DriveState && state != BoostState)
            {
                animator.speed = 1f;
                return;
            }

            var speedLimit = state == BoostState
                ? controller.Stats.BoostMaximumSpeed
                : controller.Stats.MaximumSpeed;
            var speedRatio = Mathf.Clamp01(controller.Speed / Mathf.Max(0.01f, speedLimit));
            animator.speed = Mathf.Lerp(
                minimumDrivePlaybackSpeed, maximumDrivePlaybackSpeed, speedRatio);
        }

        private void UpdateSlippers()
        {
            if (slipperSwapper == null) return;

            var stage = controller.HeatStage;
            if (stage == previousHeatStage) return;

            if (stage == SlipperHeatStage.BurnedOut)
            {
                nextSlipperIndex = slipperSwapper.SlipperCount == 0
                    ? -1
                    : (slipperSwapper.EquippedIndex + 1) % slipperSwapper.SlipperCount;
                slipperSwapper.Unequip();
            }
            else if (previousHeatStage == SlipperHeatStage.BurnedOut && nextSlipperIndex >= 0)
            {
                slipperSwapper.Equip(nextSlipperIndex);
            }

            previousHeatStage = stage;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (animator == null || collision.relativeVelocity.magnitude < minimumHitSpeed) return;
            hitAnimationRemaining = hitAnimationDuration;
            PlayState(HitReactState, 0.05f);
        }

        private void PlayState(int state, float fadeDuration)
        {
            if (state == activeState) return;
            activeState = state;
            animator.CrossFade(state, fadeDuration, 0);
        }

        private static void AddLocalRotation(Transform bone, Vector3 euler)
        {
            if (bone != null) bone.localRotation *= Quaternion.Euler(euler);
        }

        private static Transform FindDescendant(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindDescendant(root.GetChild(i), childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}

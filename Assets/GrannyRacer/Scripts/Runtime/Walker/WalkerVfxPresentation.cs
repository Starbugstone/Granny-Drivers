using System;
using UnityEngine;

namespace GrannyRacer.Walker
{
    /// <summary>
    /// Drives the walker's particle and skid-mark effects from controller state.
    ///
    /// Two things here exist because the effects hang off an imported rig whose bones carry a
    /// 100x scale and animated orientations: emitters are aimed in world space every frame
    /// rather than trusting the bone's own axes, and the skid marks are projected onto the road
    /// instead of being parented to a foot that leaves the ground.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WalkerVfxPresentation : MonoBehaviour
    {
        [SerializeField] private ArcadeWalkerController controller;
        [SerializeField] private ParticleSystem[] boostFlames = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] boostSmoke = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] slipperSmoke = Array.Empty<ParticleSystem>();
        [SerializeField] private ParticleSystem[] driftSmoke = Array.Empty<ParticleSystem>();
        [SerializeField] private TrailRenderer[] skidMarks = Array.Empty<TrailRenderer>();

        [Tooltip("Slipper sockets the skid marks are projected beneath, index-matched to skidMarks.")]
        [SerializeField] private Transform[] skidSockets = Array.Empty<Transform>();

        [Header("Skid marks")]
        [Min(0f)] [SerializeField] private float skidProbeHeight = 0.6f;
        [Min(0f)] [SerializeField] private float skidProbeDistance = 1.4f;
        [Min(0f)] [SerializeField] private float skidGroundOffset = 0.02f;

        [Tooltip("A jump this large in one frame is a respawn, not a skid, so the mark is cleared.")]
        [Min(0f)] [SerializeField] private float skidTeleportDistance = 4f;

        [Header("Drift charge colours")]
        [SerializeField] private Color driftChargingColor = new Color(0.72f, 0.72f, 0.74f, 0.55f);
        [SerializeField] private Color driftRedColor = new Color(1f, 0.16f, 0.06f, 0.85f);
        [SerializeField] private Color driftYellowColor = new Color(1f, 0.82f, 0.08f, 0.85f);
        [SerializeField] private Color driftBlueColor = new Color(0.24f, 0.55f, 1f, 0.85f);

        private readonly RaycastHit[] groundHits = new RaycastHit[8];
        private Rigidbody body;
        private bool boostEffectsActive;
        private bool slipperSmokeActive;
        private bool driftSmokeActive;
        private bool skidMarksActive;
        private DriftChargeStage driftSmokeStage = DriftChargeStage.None;

        public bool BoostEffectsActive => boostEffectsActive;
        public bool SlipperSmokeActive => slipperSmokeActive;
        public bool DriftSmokeActive => driftSmokeActive;
        public bool SkidMarksActive => skidMarksActive;

        public void Configure(ArcadeWalkerController walker, ParticleSystem[] flames,
            ParticleSystem[] rocketSmoke, ParticleSystem[] heatSmoke, ParticleSystem[] driftPlume,
            TrailRenderer[] marks, Transform[] markSockets)
        {
            controller = walker;
            boostFlames = flames ?? Array.Empty<ParticleSystem>();
            boostSmoke = rocketSmoke ?? Array.Empty<ParticleSystem>();
            slipperSmoke = heatSmoke ?? Array.Empty<ParticleSystem>();
            driftSmoke = driftPlume ?? Array.Empty<ParticleSystem>();
            skidMarks = marks ?? Array.Empty<TrailRenderer>();
            skidSockets = markSockets ?? Array.Empty<Transform>();
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ArcadeWalkerController>();
            if (controller != null) body = controller.GetComponent<Rigidbody>();
            SetParticles(boostFlames, false);
            SetParticles(boostSmoke, false);
            SetParticles(slipperSmoke, false);
            SetParticles(driftSmoke, false);
            SetTrails(false, true);
        }

        private void LateUpdate()
        {
            if (controller == null) return;

            // The exit boost fires the rockets too, so the reward reads at a glance.
            var boosting = controller.IsBoosting || controller.DriftBoostActive;
            if (boosting != boostEffectsActive)
            {
                boostEffectsActive = boosting;
                SetParticles(boostFlames, boosting);
                SetParticles(boostSmoke, boosting);
            }

            var heatSmoke = controller.HeatStage != SlipperHeatStage.Safe;
            if (heatSmoke != slipperSmokeActive)
            {
                slipperSmokeActive = heatSmoke;
                SetParticles(slipperSmoke, heatSmoke);
            }

            var drifting = controller.IsDrifting;
            if (drifting != driftSmokeActive)
            {
                driftSmokeActive = drifting;
                SetParticles(driftSmoke, drifting);
                if (drifting)
                {
                    // Applied on entry as well as on change, or the first drift of the session
                    // would emit with whatever tint the material shipped with.
                    driftSmokeStage = controller.DriftStage;
                    SetParticleColor(driftSmoke, ColorFor(driftSmokeStage));
                }
            }

            if (drifting)
            {
                var stage = controller.DriftStage;
                if (stage != driftSmokeStage)
                {
                    driftSmokeStage = stage;
                    SetParticleColor(driftSmoke, ColorFor(stage));
                }
            }

            AimEffects();
            UpdateSkidMarks();
        }

        /// <summary>
        /// Points the emitters in world space. The rig's sockets are animated, so a cone aimed
        /// down a bone axis would spray the flames into the road on some frames.
        /// </summary>
        private void AimEffects()
        {
            var backwards = -controller.transform.forward;
            if (boostEffectsActive)
            {
                AimParticles(boostFlames, backwards);
                AimParticles(boostSmoke, backwards);
            }

            if (slipperSmokeActive) AimParticles(slipperSmoke, Vector3.up);
            if (driftSmokeActive) AimParticles(driftSmoke, Vector3.up);
        }

        private void UpdateSkidMarks()
        {
            var marking = controller.IsSkidding && controller.IsGrounded;
            var count = Mathf.Min(skidMarks.Length, skidSockets.Length);
            for (var i = 0; i < count; i++)
            {
                var trail = skidMarks[i];
                var socket = skidSockets[i];
                if (trail == null || socket == null) continue;

                var origin = socket.position + Vector3.up * skidProbeHeight;
                if (!TryFindGround(origin, skidProbeHeight + skidProbeDistance, out var hit))
                {
                    trail.emitting = false;
                    continue;
                }

                var point = hit.point + hit.normal * skidGroundOffset;
                if ((point - trail.transform.position).sqrMagnitude
                    > skidTeleportDistance * skidTeleportDistance)
                {
                    trail.Clear();
                }

                // TransformZ alignment lays the ribbon flat once the transform's forward is the
                // ground normal, which keeps the mark on the road instead of edge-on.
                trail.transform.SetPositionAndRotation(point,
                    Quaternion.LookRotation(hit.normal, controller.transform.forward));
                trail.emitting = marking;
            }

            skidMarksActive = marking;
        }

        private bool TryFindGround(Vector3 origin, float distance, out RaycastHit hit)
        {
            var count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits, distance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var best = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                // The probe starts inside the walker, so its own colliders have to be skipped.
                if (body != null && groundHits[i].rigidbody == body) continue;
                if (groundHits[i].distance >= bestDistance) continue;
                bestDistance = groundHits[i].distance;
                best = i;
            }

            if (best < 0)
            {
                hit = default;
                return false;
            }

            hit = groundHits[best];
            return true;
        }

        private Color ColorFor(DriftChargeStage stage)
        {
            switch (stage)
            {
                case DriftChargeStage.Red: return driftRedColor;
                case DriftChargeStage.Yellow: return driftYellowColor;
                case DriftChargeStage.Blue: return driftBlueColor;
                default: return driftChargingColor;
            }
        }

        private static void AimParticles(ParticleSystem[] systems, Vector3 direction)
        {
            var rotation = Quaternion.LookRotation(direction,
                Mathf.Abs(direction.y) > 0.99f ? Vector3.forward : Vector3.up);
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                systems[i].transform.rotation = rotation;
            }
        }

        private static void SetParticleColor(ParticleSystem[] systems, Color color)
        {
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i] == null) continue;
                var main = systems[i].main;
                main.startColor = color;
            }
        }

        private static void SetParticles(ParticleSystem[] systems, bool playing)
        {
            for (var i = 0; i < systems.Length; i++)
            {
                var particles = systems[i];
                if (particles == null) continue;
                if (playing)
                {
                    particles.Play(true);
                }
                else
                {
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void SetTrails(bool emitting, bool clear)
        {
            for (var i = 0; i < skidMarks.Length; i++)
            {
                var trail = skidMarks[i];
                if (trail == null) continue;
                trail.emitting = emitting;
                if (clear) trail.Clear();
            }
        }
    }
}

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
        [Tooltip("Skid ribbons, socket-major: the first skidRibbonsPerSocket entries belong to "
            + "the first socket, and so on.")]
        [SerializeField] private TrailRenderer[] skidMarks = Array.Empty<TrailRenderer>();

        [Tooltip("Slipper sockets the skid marks are projected beneath.")]
        [SerializeField] private Transform[] skidSockets = Array.Empty<Transform>();

        [Header("Skid marks")]
        [Tooltip("Ribbons available to each slipper. Each unbroken skid takes one and holds it "
            + "until the skid ends, so this is how many skids can be fading at once before the "
            + "oldest is cut short.")]
        [Min(1)] [SerializeField] private int skidRibbonsPerSocket = 5;

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
        private SkidMarkPool[] skidPools = Array.Empty<SkidMarkPool>();
        private int ribbonsPerSocket = 1;
        private float skidRecycleSeconds = 3.25f;
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
            TrailRenderer[] marks, Transform[] markSockets, int marksPerSocket)
        {
            controller = walker;
            boostFlames = flames ?? Array.Empty<ParticleSystem>();
            boostSmoke = rocketSmoke ?? Array.Empty<ParticleSystem>();
            slipperSmoke = heatSmoke ?? Array.Empty<ParticleSystem>();
            driftSmoke = driftPlume ?? Array.Empty<ParticleSystem>();
            skidMarks = marks ?? Array.Empty<TrailRenderer>();
            skidSockets = markSockets ?? Array.Empty<Transform>();
            skidRibbonsPerSocket = Mathf.Max(1, marksPerSocket);
        }

        private void Awake()
        {
            if (controller == null) controller = GetComponent<ArcadeWalkerController>();
            if (controller != null) body = controller.GetComponent<Rigidbody>();
            SetParticles(boostFlames, false);
            SetParticles(boostSmoke, false);
            SetParticles(slipperSmoke, false);
            SetParticles(driftSmoke, false);
            SleepAllTrails();
            BuildSkidPools();
        }

        private void BuildSkidPools()
        {
            ribbonsPerSocket = Mathf.Max(1, skidRibbonsPerSocket);
            var socketCount = Mathf.Min(skidSockets.Length, skidMarks.Length / ribbonsPerSocket);
            skidPools = socketCount < 1 ? Array.Empty<SkidMarkPool>() : new SkidMarkPool[socketCount];
            for (var i = 0; i < skidPools.Length; i++)
            {
                skidPools[i] = new SkidMarkPool(ribbonsPerSocket);
            }

            for (var i = 0; i < skidMarks.Length; i++)
            {
                if (skidMarks[i] == null) continue;
                // Taken from the ribbons themselves so there is only one place to tune the fade.
                // The margin keeps the explicit clear behind the renderer's own fade, so it only
                // ever catches a mark the renderer failed to age out.
                skidRecycleSeconds = skidMarks[i].time + 0.25f;
                break;
            }

        }

        private void LateUpdate()
        {
            if (controller == null) return;

            // The exit boost fires the rockets too, so the reward reads at a glance.
            var boosting = controller.IsBoosting || controller.DriftBoostActive
                || controller.IsStartBoostActive;
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

            // Gated on contact as well as on the drift: the slippers only smoke when they are
            // scrubbing tarmac, so a drift that hops a kerb stops smoking until it lands. This
            // is also what keeps the hop that arms a drift completely clean — the drift itself
            // does not exist until the landing.
            var drifting = (controller.IsDrifting || controller.IsStartSkidding)
                && controller.IsGrounded;
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

        /// <summary>
        /// Paints the current skid. Only the ribbon a slipper is actively drawing into is moved;
        /// released ribbons are left exactly where they were, so their marks sit on the road and
        /// fade out on the TrailRenderer's own clock rather than following Granny around.
        /// </summary>
        private void UpdateSkidMarks()
        {
            var marking = controller.IsSkidding && controller.IsGrounded;
            var now = Time.time;
            var painting = false;

            for (var socketIndex = 0; socketIndex < skidPools.Length; socketIndex++)
            {
                var socket = skidSockets[socketIndex];
                var pool = skidPools[socketIndex];

                var expired = pool.TryRecycle(now, skidRecycleSeconds);
                if (expired != SkidMarkPool.NoRibbon)
                {
                    var stale = Ribbon(socketIndex, expired);
                    if (stale != null) Retire(stale);
                }

                // No socket, no skid, or a slipper with nothing under it (mid-hop, off the edge
                // of the road) all end the current ribbon rather than pausing it.
                if (socket == null || !marking
                    || !TryFindGround(socket.position + Vector3.up * skidProbeHeight,
                        skidProbeHeight + skidProbeDistance, out var hit))
                {
                    StopRibbon(pool, socketIndex, now);
                    continue;
                }

                var ribbon = Ribbon(socketIndex, pool.Acquire(now, out var started));
                if (ribbon == null) continue;

                var point = hit.point + hit.normal * skidGroundOffset;
                // TransformZ alignment lays the ribbon flat once the transform's forward is the
                // ground normal, which keeps the mark on the road instead of edge-on.
                var rotation = Quaternion.LookRotation(hit.normal, controller.transform.forward);

                if (started)
                {
                    // Woken, emptied and teleported before it is allowed to record anything. A
                    // recycled ribbon may still hold the tail of an older mark, and joining that
                    // to the new skid would draw a straight line across the track.
                    ribbon.gameObject.SetActive(true);
                    ribbon.Clear();
                    ribbon.transform.SetPositionAndRotation(point, rotation);
                    ribbon.emitting = true;
                }
                else
                {
                    if ((point - ribbon.transform.position).sqrMagnitude
                        > skidTeleportDistance * skidTeleportDistance)
                    {
                        // A respawn, not a skid.
                        ribbon.Clear();
                    }

                    ribbon.transform.SetPositionAndRotation(point, rotation);
                }

                painting = true;
            }

            skidMarksActive = painting;
        }

        private void StopRibbon(SkidMarkPool pool, int socketIndex, float now)
        {
            var released = pool.Release(now);
            if (released == SkidMarkPool.NoRibbon) return;
            var ribbon = Ribbon(socketIndex, released);
            // Left active and where it is, so the mark it drew stays on the road and fades. It
            // is not moving any more, so it records nothing further.
            if (ribbon != null) ribbon.emitting = false;
        }

        /// <summary>
        /// Puts a faded-out ribbon back to sleep.
        ///
        /// Deactivating it is not just tidiness. A TrailRenderer records points from its own
        /// movement whether or not <c>emitting</c> is set, so an idle ribbon left awake and
        /// carried around by the racer quietly draws her whole route. Only a disabled one is
        /// guaranteed to record nothing.
        /// </summary>
        private static void Retire(TrailRenderer ribbon)
        {
            ribbon.emitting = false;
            ribbon.Clear();
            ribbon.gameObject.SetActive(false);
        }

        private TrailRenderer Ribbon(int socketIndex, int slot)
        {
            var index = socketIndex * ribbonsPerSocket + slot;
            return index >= 0 && index < skidMarks.Length ? skidMarks[index] : null;
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

        private void SleepAllTrails()
        {
            for (var i = 0; i < skidMarks.Length; i++)
            {
                if (skidMarks[i] != null) Retire(skidMarks[i]);
            }
        }
    }
}

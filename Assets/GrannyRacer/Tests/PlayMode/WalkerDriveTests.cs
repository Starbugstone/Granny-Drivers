using System.Collections;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    /// <summary>
    /// Drives the real POC scene with a virtual keyboard. Guards two faults that both present
    /// as "the walker steers but will not accelerate": an inverted road MeshCollider, and PhysX
    /// friction cancelling the controller's own drive force.
    /// </summary>
    public sealed class WalkerDriveTests : InputTestFixture
    {
        private Keyboard keyboard;
        private ArcadeWalkerController racer;
        private RaceController race;
        private Rigidbody body;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        /// <summary>
        /// Loaded from inside each test rather than [UnitySetUp]: the scene's PlayerRacerInput
        /// resolves its bindings in Awake, and that has to happen after InputTestFixture has
        /// installed the virtual keyboard or the actions stay bound to a stale device.
        /// </summary>
        private IEnumerator LoadPocScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("POC_QuietSunday", LoadSceneMode.Single);
            racer = FindInActiveScene<ArcadeWalkerController>();
            race = FindInActiveScene<RaceController>();
            Assert.That(racer, Is.Not.Null);
            Assert.That(race, Is.Not.Null);
            body = racer.GetComponent<Rigidbody>();

            while (race.State == RaceState.Countdown)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ThrottleAcceleratesTheWalkerForward()
        {
            yield return LoadPocScene();

            var startPosition = body.position;
            Press(keyboard.wKey);
            yield return null;

            // ~0.6 s: long enough to approach the 15 m/s cap, short enough that the walker
            // cannot reach the first corner's barrier ~25 m away.
            for (var i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            var travelled = Vector3.Distance(body.position, startPosition);
            Assert.That(racer.IsGrounded, Is.True, "Walker must be grounded on the road.");
            Assert.That(racer.Speed, Is.GreaterThan(8f),
                $"Holding W must build speed. Speed was {racer.Speed:0.00} m/s after 30 fixed steps.");
            Assert.That(travelled, Is.GreaterThan(2f),
                $"Walker only travelled {travelled:0.00} m while accelerating.");
        }

        /// <summary>
        /// The collider is frictionless, so without explicit rolling resistance a walker with no
        /// throttle would coast forever. Driven by setting velocity rather than by input, so the
        /// check does not depend on input-event timing.
        /// </summary>
        [UnityTest]
        public IEnumerator WalkerCoastsDownWithNoThrottle()
        {
            yield return LoadPocScene();

            body.linearVelocity = racer.transform.forward * 12f;
            yield return new WaitForFixedUpdate();
            var movingSpeed = racer.Speed;
            Assert.That(movingSpeed, Is.GreaterThan(8f), "Walker must be moving before coasting.");

            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(racer.Speed, Is.LessThan(movingSpeed - 2f),
                "With no throttle held, rolling resistance must bleed speed. "
                + $"{movingSpeed:0.00} -> {racer.Speed:0.00} m/s.");
        }

        [UnityTest]
        public IEnumerator WalkerSettlesOnTopOfTheRoadNotBeneathIt()
        {
            yield return LoadPocScene();

            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            // Cast down from well above the walker. A road wound inside-out is invisible to a
            // downward ray (queriesHitBackfaces is off) and lets the walker drop through it.
            var origin = body.position + Vector3.up * 20f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 40f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var roadY = float.NaN;
            for (var i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider.name == "Generated Road") roadY = hits[i].point.y;
            }

            Assert.That(float.IsNaN(roadY), Is.False,
                "A downward ray must hit the road surface — an inverted road mesh is invisible "
                + "to it and the walker falls through.");
            Assert.That(body.position.y, Is.GreaterThan(roadY),
                $"Walker settled at y={body.position.y:0.00}, below the road at y={roadY:0.00}.");
        }

        [UnityTest]
        public IEnumerator WalkerColliderIsFrictionless()
        {
            yield return LoadPocScene();

            var material = racer.GetComponent<BoxCollider>().sharedMaterial;
            Assert.That(material, Is.Not.Null,
                "Walker collider needs a frictionless material; PhysX friction otherwise fights "
                + "the controller's own drive force.");
            Assert.That(material.staticFriction, Is.EqualTo(0f).Within(0.001f));
            Assert.That(material.dynamicFriction, Is.EqualTo(0f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator JumpButtonAddsAControlledUpwardImpulse()
        {
            yield return LoadPocScene();

            Press(keyboard.leftShiftKey);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(racer.VerticalSpeed, Is.GreaterThan(2f),
                $"Jump must create upward velocity, but vertical speed was {racer.VerticalSpeed:0.00} m/s.");
            Assert.That(racer.IsJumping, Is.True);
        }

        [UnityTest]
        public IEnumerator BrakingAndSteeringAtSpeedStartsASkid()
        {
            yield return LoadPocScene();

            body.linearVelocity = racer.transform.forward * 10f;
            Press(keyboard.sKey);
            Press(keyboard.dKey);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(racer.IsSkidding, Is.True,
                "Braking and steering above the skid speed must enter the skid presentation state.");
        }

        /// <summary>
        /// Holds the walker on a straight line at a fixed speed for the given number of physics
        /// steps. The drift rules themselves are covered by DriftModelTests; what these PlayMode
        /// tests check is that real input reaches them through the scene, so the walker is kept
        /// pinned rather than allowed to carve into the first barrier mid-charge.
        /// </summary>
        private IEnumerator HoldOnRails(int steps, float speed)
        {
            var rotation = body.rotation;
            var heading = racer.transform.forward;
            for (var i = 0; i < steps; i++)
            {
                body.rotation = rotation;
                body.angularVelocity = Vector3.zero;
                body.linearVelocity = heading * speed + Vector3.up * body.linearVelocity.y;
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator HoppingWhileSteeringStartsAChargingDrift()
        {
            yield return LoadPocScene();

            body.linearVelocity = racer.transform.forward * 8f;
            Press(keyboard.dKey);
            yield return null;
            Press(keyboard.leftShiftKey);
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(racer.IsDrifting, Is.True,
                "Hopping while steering at speed must commit the walker to a drift.");
            Assert.That(racer.DriftDirection, Is.EqualTo(1), "Steering right must drift right.");

            // Long enough to land the hop and then bank the first tier on the ground.
            yield return HoldOnRails(100, 8f);

            Assert.That(racer.IsDrifting, Is.True,
                "Holding Left Shift must keep the drift alive after landing.");
            Assert.That(racer.DriftStage, Is.Not.EqualTo(DriftChargeStage.None),
                $"The drift charged for {racer.DriftCharge:0.00}s without reaching a tier.");
            Assert.That(racer.IsSkidding, Is.True, "A drift must read as a skid for presentation.");
        }

        [UnityTest]
        public IEnumerator ReleasingAChargedDriftGrantsASpeedBoost()
        {
            yield return LoadPocScene();

            body.linearVelocity = racer.transform.forward * 8f;
            Press(keyboard.wKey);
            Press(keyboard.dKey);
            yield return null;
            Press(keyboard.leftShiftKey);
            yield return null;

            yield return HoldOnRails(100, 8f);

            Assert.That(racer.DriftStage, Is.Not.EqualTo(DriftChargeStage.None),
                "The drift must reach a tier before the release can be measured.");

            var speedBefore = racer.Speed;

            // Whole-device state rather than Release(): a delta-state event on a single-bit key
            // control throws inside InputTestFixture here. This keeps W and D held and lifts
            // only Left Shift, which is what ends the drift.
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.D));
            InputSystem.Update();
            yield return null;
            yield return new WaitForFixedUpdate();

            Assert.That(racer.IsDrifting, Is.False, "Releasing Left Shift must end the drift.");
            Assert.That(racer.DriftBoostActive, Is.True, "The exit boost must be running.");
            Assert.That(racer.DriftBoostStage, Is.Not.EqualTo(DriftChargeStage.None));
            Assert.That(racer.Speed, Is.GreaterThan(speedBefore + 1f),
                $"The exit boost must add speed. {speedBefore:0.00} -> {racer.Speed:0.00} m/s.");
        }

        /// <summary>
        /// The regression that mattered: the boost VFX were wired, playing, and invisible,
        /// because emitters scaled to cancel the rig's 100x bones rendered at about a
        /// millimetre. Wiring assertions could not catch that — this measures live particles.
        /// </summary>
        [UnityTest]
        public IEnumerator BoostingActuallyEmitsParticlesAtAVisibleSize()
        {
            yield return LoadPocScene();

            var flame = FindParticles("Boost_Flame_L");
            var smoke = FindParticles("Boost_Smoke_L");
            Assert.That(flame, Is.Not.Null, "The scene needs a Boost_Flame_L emitter.");
            Assert.That(smoke, Is.Not.Null, "The scene needs a Boost_Smoke_L emitter.");
            Assert.That(flame.isEmitting, Is.False, "Flames must be off before the boost.");

            Press(keyboard.wKey);
            Press(keyboard.spaceKey);
            yield return null;
            yield return null;

            Assert.That(racer.IsBoosting, Is.True, "Space must engage the boost.");
            Assert.That(flame.isEmitting, Is.True, "Boosting must start the rocket flames.");

            // Half a second of simulated time, not a frame count: batch-mode frames are around
            // a millisecond, so 30 of them give a 30-per-second emitter almost no chance to
            // spawn anything and the assertion below turns flaky.
            yield return new WaitForSeconds(0.5f);

            Assert.That(flame.particleCount, Is.GreaterThan(0),
                "The flame emitter is playing but has produced no particles.");
            Assert.That(smoke.particleCount, Is.GreaterThan(0),
                "The rocket smoke emitter is playing but has produced no particles.");

            // A hundredth of a metre across is the invisible case; real flames are ~0.28 m.
            var bounds = flame.GetComponent<ParticleSystemRenderer>().bounds;
            Assert.That(bounds.size.magnitude, Is.GreaterThan(0.1f),
                $"Live flame particles span only {bounds.size.magnitude:0.0000} m — too small to see.");
        }

        private static ParticleSystem FindParticles(string name)
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var systems = racer.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < systems.Length; i++)
            {
                if (systems[i].name == name) return systems[i];
            }

            return null;
        }

        private static T FindInActiveScene<T>() where T : Component
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var component = roots[i].GetComponentInChildren<T>(true);
                if (component != null) return component;
            }

            return null;
        }
    }
}

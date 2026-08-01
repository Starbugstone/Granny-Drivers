using System.Collections;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
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

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
    public sealed class RaceStartPlayModeTests : InputTestFixture
    {
        private Keyboard keyboard;
        private RaceController race;
        private ArcadeWalkerController racer;
        private WalkerVfxPresentation vfx;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        private IEnumerator LoadCountdown()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("POC_QuietSunday", LoadSceneMode.Single);
            race = FindInActiveScene<RaceController>();
            racer = FindInActiveScene<ArcadeWalkerController>();
            vfx = FindInActiveScene<WalkerVfxPresentation>();
            Assert.That(race, Is.Not.Null);
            Assert.That(racer, Is.Not.Null);
            Assert.That(vfx, Is.Not.Null);
            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));

            // Add the virtual device after the scene's action map exists. This avoids retaining
            // a control whose backing state Unity may replace while loading the scene.
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoldingThrottleTooEarlyProducesWheelspinAndSmoke()
        {
            yield return LoadCountdown();
            Press(keyboard.wKey);

            while (race.State == RaceState.Countdown) yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(race.StartOutcome, Is.EqualTo(RaceStartOutcome.Skid));
            Assert.That(racer.IsStartSkidding, Is.True,
                "An early rev must still be inside its slowed, swerving launch state.");
            Assert.That(racer.IsSkidding, Is.True,
                "Wheelspin must feed the shared skid-mark presentation state.");
            Assert.That(vfx.DriftSmokeActive, Is.True,
                "Wheelspin must turn on the existing slipper smoke particles.");
        }

        [UnityTest]
        public IEnumerator PressingInsidePerfectWindowProducesTurboAndRocketEffects()
        {
            yield return LoadCountdown();
            while (race.CountdownRemaining > 2.7f) yield return null;
            Press(keyboard.wKey);

            while (race.State == RaceState.Countdown) yield return null;
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(race.StartOutcome, Is.EqualTo(RaceStartOutcome.Turbo));
            Assert.That(racer.Speed, Is.GreaterThan(5f),
                "The perfect start must apply its configured velocity-change reward.");
            Assert.That(racer.IsStartBoostActive, Is.True);
            Assert.That(vfx.BoostEffectsActive, Is.True,
                "The perfect start must fire the existing rocket particles.");
        }

        private static T FindInActiveScene<T>() where T : Component
        {
            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }
    }
}

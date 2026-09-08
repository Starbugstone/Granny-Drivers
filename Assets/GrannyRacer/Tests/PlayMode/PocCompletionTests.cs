using System.Collections;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class PocCompletionTests
    {
        private RaceController race;
        private ArcadeWalkerController walker;

        [UnitySetUp]
        public IEnumerator Load()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("POC_QuietSunday");
            race = Object.FindAnyObjectByType<RaceController>();
            walker = Object.FindAnyObjectByType<ArcadeWalkerController>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTest]
        public IEnumerator BlenderSceneryPreservesItsImportedUpAxis()
        {
            var houses = 0;
            var dashes = 0;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>())
            {
                if (renderer.name == "ENV_House")
                {
                    houses++;
                    Assert.That(renderer.bounds.size.y, Is.InRange(5.1f, 5.3f), "House roof must point up.");
                }
                else if (renderer.name == "PRP_RoadDash")
                {
                    dashes++;
                    Assert.That(renderer.bounds.size.y, Is.LessThan(.04f), "Road paint must lie flat.");
                }
            }
            Assert.That(houses, Is.GreaterThan(0));
            Assert.That(dashes, Is.GreaterThan(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ThreeOrderedLapsFinishAndCanRestartWithoutReloading()
        {
            while (race.State == RaceState.Countdown) yield return null;
            var checkpoints = Object.FindObjectsByType<RaceCheckpoint>();
            var count = checkpoints.Length;
            var ordered = new Transform[count];
            foreach (var checkpoint in checkpoints)
            {
                var index = checkpoint.name.StartsWith("Lap") ? 0
                    : int.Parse(checkpoint.name.Substring("Checkpoint ".Length));
                ordered[index] = checkpoint.transform;
            }
            Assert.That(race.TryPassCheckpoint(0, ordered[0], walker), Is.False, "Cannot skip the course.");
            for (var lap = 0; lap < race.LapTarget; lap++)
            {
                for (var index = 1; index < count; index++)
                    Assert.That(race.TryPassCheckpoint(index, ordered[index], walker), Is.True);
                Assert.That(race.TryPassCheckpoint(0, ordered[0], walker), Is.True);
            }
            Assert.That(race.State, Is.EqualTo(RaceState.Finished));
            var finish = race.FinishTime;
            yield return null;
            Assert.That(race.FinishTime, Is.EqualTo(finish));
            race.StartRace();
            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(race.CurrentLap, Is.EqualTo(1));
            Assert.That(walker.Heat, Is.Zero);
            Assert.That(race.FinishTime, Is.Zero);
        }

        [UnityTest]
        public IEnumerator PauseFreezesCountdownAndResumesIt()
        {
            var remaining = race.CountdownRemaining;
            race.TogglePause();
            var position = walker.GetComponent<Rigidbody>().position;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(race.CountdownRemaining, Is.EqualTo(remaining));
            Assert.That(walker.GetComponent<Rigidbody>().position, Is.EqualTo(position));
            race.TogglePause();
            yield return null;
            yield return null;
            Assert.That(race.CountdownRemaining, Is.LessThan(remaining));
        }

        [UnityTest]
        public IEnumerator FallingBelowTheCourseRecoversToTheLastCheckpoint()
        {
            while (race.State == RaceState.Countdown) yield return null;
            var body = walker.GetComponent<Rigidbody>();
            var safe = body.position + Vector3.up * .2f;
            walker.SetResetPose(safe, Quaternion.identity);
            body.position = safe + Vector3.down * 30;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(Vector3.Distance(body.position, safe), Is.LessThan(.25f));
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(2f));
        }

        [UnityTest]
        public IEnumerator TuningUsesAnIndependentCopyAndRestoresTheAsset()
        {
            var original = walker.Handling;
            var originalAcceleration = original.acceleration;
            walker.BeginSessionTuning();
            walker.Handling.acceleration = 31.5f;
            walker.RefreshHandling();
            Assert.That(walker.Handling, Is.Not.SameAs(original));
            Assert.That(original.acceleration, Is.EqualTo(originalAcceleration));
            Assert.That(walker.Stats.Acceleration, Is.EqualTo(31.5f));
            walker.RestoreSessionTuning();
            Assert.That(walker.Handling, Is.SameAs(original));
            Assert.That(walker.Stats.Acceleration, Is.EqualTo(originalAcceleration));
            yield return null;
        }
    }
}

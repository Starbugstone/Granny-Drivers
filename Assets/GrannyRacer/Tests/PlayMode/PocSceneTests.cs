using System.Collections;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class PocSceneTests
    {
        [UnitySetUp]
        public IEnumerator LoadPocScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("POC_QuietSunday", LoadSceneMode.Single);
            var race = FindInActiveScene<RaceController>();
            Assert.That(race, Is.Not.Null);
            while (!race.IsInitialized)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator RacerAndRaceControllerSpawnConfigured()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var race = FindInActiveScene<RaceController>();

            Assert.That(racer, Is.Not.Null);
            Assert.That(race, Is.Not.Null);
            Assert.That(racer.Handling, Is.Not.Null);
            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualResetRestoresTheLatestResetPose()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var resetPosition = new Vector3(5f, 1.2f, 7f);
            var body = racer.GetComponent<Rigidbody>();
            racer.SetResetPose(resetPosition, Quaternion.Euler(0f, 90f, 0f));
            body.position = new Vector3(-20f, 8f, -20f);
            racer.ResetToSpawn();
            Assert.That(Vector3.Distance(body.position, resetPosition), Is.LessThan(0.1f),
                "Reset must update the authoritative Rigidbody immediately.");
            yield return new WaitForFixedUpdate();

            Assert.That(Vector3.Distance(body.position, resetPosition), Is.LessThan(0.1f));
        }

        [UnityTest]
        public IEnumerator OrderedCheckpointsAdvanceRaceAndUpdateReset()
        {
            var race = FindInActiveScene<RaceController>();
            var checkpoints = FindAllInActiveScene<RaceCheckpoint>();
            Assert.That(checkpoints, Has.Length.EqualTo(4));

            while (race.State == RaceState.Countdown)
            {
                yield return null;
            }

            RaceCheckpoint checkpointOne = null;
            for (var i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i].name == "Checkpoint 1") checkpointOne = checkpoints[i];
            }

            Assert.That(checkpointOne, Is.Not.Null);
            var racer = FindInActiveScene<ArcadeWalkerController>();
            Assert.That(race.TryPassCheckpoint(1, checkpointOne.transform, racer), Is.True);
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

        private static T[] FindAllInActiveScene<T>() where T : Component
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var results = new System.Collections.Generic.List<T>();
            for (var i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results.ToArray();
        }
    }
}

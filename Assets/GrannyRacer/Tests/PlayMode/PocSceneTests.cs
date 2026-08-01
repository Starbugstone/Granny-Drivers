using System.Collections;
using System.Text;
using GrannyRacer.Audio;
using GrannyRacer.Camera;
using GrannyRacer.Characters;
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
        public IEnumerator MergedModelSlippersAndVoiceAreWiredIntoTheRacer()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var animator = racer.GetComponentInChildren<Animator>(true);
            var slippers = racer.GetComponentInChildren<SlipperSwapper>(true);
            var voice = racer.GetComponent<GrannyVoice>();

            Assert.That(animator, Is.Not.Null, "The imported Granny walker model needs an Animator.");
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null,
                "The POC model needs the generated locomotion controller.");
            Assert.That(slippers, Is.Not.Null, "The model needs the merged slipper swapper.");
            Assert.That(slippers.SlipperCount, Is.EqualTo(3));
            Assert.That(voice, Is.Not.Null, "The racer needs the merged Granny voice component.");
            Assert.That(Resources.LoadAll<AudioClip>("Audio/Granny/Collision"), Has.Length.EqualTo(5));
            Assert.That(Resources.LoadAll<AudioClip>("Audio/Granny/Input"), Has.Length.EqualTo(5));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoostHeatAndSkidEffectsAreWiredToRigSockets()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var vfx = racer.GetComponent<WalkerVfxPresentation>();
            var particles = racer.GetComponentsInChildren<ParticleSystem>(true);
            var trails = racer.GetComponentsInChildren<TrailRenderer>(true);

            Assert.That(vfx, Is.Not.Null, "The racer needs state-driven boost, heat, and skid VFX.");
            Assert.That(particles, Has.Length.EqualTo(8),
                "Two boost flames, two rocket smoke emitters, two slipper heat smoke emitters, "
                + "and two drift plumes are required.");
            Assert.That(trails, Has.Length.EqualTo(2), "Each slipper needs its own skid-mark trail.");
            yield return null;

            for (var i = 0; i < particles.Length; i++)
            {
                Assert.That(Vector3.Distance(particles[i].transform.position, racer.transform.position),
                    Is.LessThan(3f), $"{particles[i].name} must stay attached to the racer rig.");

                // Guards the fault that made the first VFX pass invisible: emitters parented to
                // the rig's 100x bones were scaled down to compensate, but a Local-scaled
                // particle system ignores its parents, so everything rendered millimetre-sized.
                var main = particles[i].main;
                Assert.That(main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Local),
                    $"{particles[i].name} must use Local particle scaling.");
                Assert.That(particles[i].transform.localScale.x, Is.EqualTo(1f).Within(0.001f),
                    $"{particles[i].name} scale {particles[i].transform.localScale.x} shrinks its "
                    + "particles below visibility.");
                Assert.That(main.startSize.constant, Is.GreaterThan(0.05f),
                    $"{particles[i].name} start size {main.startSize.constant} m is too small to see.");
                Assert.That(particles[i].GetComponent<ParticleSystemRenderer>().sharedMaterial,
                    Is.Not.Null, $"{particles[i].name} has no material and would not render.");
            }
            for (var i = 0; i < trails.Length; i++)
            {
                Assert.That(Vector3.Distance(trails[i].transform.position, racer.transform.position),
                    Is.LessThan(3f), $"{trails[i].name} must track a slipper's ground contact.");
                Assert.That(trails[i].time, Is.EqualTo(4f).Within(0.01f),
                    $"{trails[i].name} must linger for four seconds.");
                Assert.That(trails[i].sharedMaterial, Is.Not.Null,
                    $"{trails[i].name} has no material and would not render.");
            }
        }

        [UnityTest]
        public IEnumerator AnimatedModelRemainsAtMetreScale()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var animator = racer.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);

            yield return null;
            yield return null;

            var allRenderers = racer.GetComponentsInChildren<Renderer>(true);
            var renderers = new System.Collections.Generic.List<Renderer>();
            for (var i = 0; i < allRenderers.Length; i++)
            {
                if (allRenderers[i] is ParticleSystemRenderer || allRenderers[i] is TrailRenderer) continue;
                renderers.Add(allRenderers[i]);
            }
            Assert.That(renderers, Is.Not.Empty);
            var bounds = renderers[0].bounds;
            var details = new StringBuilder();
            for (var i = 0; i < renderers.Count; i++)
            {
                if (i > 0) bounds.Encapsulate(renderers[i].bounds);
                details.Append(renderers[i].name)
                    .Append(" size=").Append(renderers[i].bounds.size)
                    .Append(" scale=").Append(renderers[i].transform.lossyScale)
                    .AppendLine();
                var ancestor = renderers[i].transform;
                while (ancestor != null && ancestor != racer.transform)
                {
                    details.Append("  ").Append(ancestor.name)
                        .Append(" localScale=").Append(ancestor.localScale)
                        .Append(" localPosition=").Append(ancestor.localPosition)
                        .AppendLine();
                    ancestor = ancestor.parent;
                }
            }

            var largestDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Assert.That(largestDimension, Is.GreaterThan(0.5f).And.LessThan(4f),
                $"Animated Granny bounds must stay at metre scale, but were {bounds.size}.\n{details}");
        }

        [UnityTest]
        public IEnumerator AnimatedModelRemainsUpright()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var animator = racer.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);

            yield return null;
            yield return null;

            var renderers = racer.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Assert.That(renderers, Is.Not.Empty);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var details = new StringBuilder();
            var node = animator.transform;
            while (node != null && node != racer.transform)
            {
                details.Append(node.name)
                    .Append(" localRotation=").Append(node.localEulerAngles)
                    .Append(" worldUp=").Append(node.up)
                    .AppendLine();
                node = node.parent;
            }

            Assert.That(bounds.size.y, Is.GreaterThan(bounds.size.x)
                    .And.GreaterThan(bounds.size.z),
                $"Animated Granny must remain upright, but bounds were {bounds.size}.\n{details}");
        }

        [UnityTest]
        public IEnumerator IdleAnimationMovesTheGrannyRig()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var animator = racer.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            var head = FindDescendant(animator.transform, "Head");
            Assert.That(head, Is.Not.Null, "The imported Generic rig must expose its Head bone.");

            yield return null;
            yield return null;
            var firstRotation = head.localRotation;
            yield return new WaitForSeconds(0.4f);
            var rotationChange = Quaternion.Angle(firstRotation, head.localRotation);

            Assert.That(rotationChange, Is.GreaterThan(0.25f),
                $"Idle must visibly animate the rig; Head moved only {rotationChange:0.000} degrees.");
        }

        [UnityTest]
        public IEnumerator CameraUsesClosePocFraming()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var followCamera = FindInActiveScene<WalkerFollowCamera>();
            Assert.That(followCamera, Is.Not.Null);

            yield return null;

            Assert.That(followCamera.Offset.magnitude, Is.LessThan(5f),
                "The POC camera should remain close enough for the Granny animation to read.");
            Assert.That(Vector3.Distance(followCamera.transform.position, racer.transform.position),
                Is.LessThan(5.5f));
        }

        [UnityTest]
        public IEnumerator DriveAnimationMovesLegsFasterWithRacerSpeed()
        {
            var racer = FindInActiveScene<ArcadeWalkerController>();
            var animator = racer.GetComponentInChildren<Animator>(true);
            var foot = FindDescendant(animator.transform, "Foot_L");
            Assert.That(animator, Is.Not.Null);
            Assert.That(foot, Is.Not.Null);

            var body = racer.GetComponent<Rigidbody>();
            yield return new WaitForSeconds(0.9f);
            body.linearVelocity = racer.transform.forward * (racer.Handling.maximumSpeed * 0.75f);
            yield return new WaitForSeconds(0.2f);

            var driveIsActive = animator.GetCurrentAnimatorStateInfo(0).IsName("Drive")
                || animator.GetNextAnimatorStateInfo(0).IsName("Drive");
            Assert.That(driveIsActive, Is.True,
                "Moving and steering must retain the Drive leg cycle.");
            Assert.That(animator.speed, Is.GreaterThan(1f),
                "Drive animation playback should increase with racer speed.");

            var firstRotation = foot.localRotation;
            yield return new WaitForSeconds(0.2f);
            var rotationChange = Quaternion.Angle(firstRotation, foot.localRotation);
            Assert.That(rotationChange, Is.GreaterThan(0.25f),
                $"Drive must visibly animate the legs; Foot_L moved only {rotationChange:0.000} degrees.");
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

            // Count is not asserted exactly: the layout is retuned between playtests. What
            // must hold is that the generator numbered them contiguously from the lap
            // trigger, because RaceProgress advances strictly through those indices.
            Assert.That(checkpoints, Has.Length.GreaterThanOrEqualTo(4));
            var names = new System.Collections.Generic.List<string>();
            for (var i = 0; i < checkpoints.Length; i++) names.Add(checkpoints[i].name);
            Assert.That(names, Contains.Item("Lap Trigger (Checkpoint 0)"));
            for (var i = 1; i < checkpoints.Length; i++)
            {
                Assert.That(names, Contains.Item($"Checkpoint {i}"),
                    $"Checkpoint {i} is missing, so the ordered sequence has a hole in it.");
            }

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

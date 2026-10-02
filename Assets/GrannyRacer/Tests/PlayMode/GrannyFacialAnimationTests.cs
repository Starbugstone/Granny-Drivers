using System.Collections;
using GrannyRacer.Walker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GrannyRacer.Tests.PlayMode
{
    public sealed class GrannyFacialAnimationTests
    {
        [UnityTest]
        public IEnumerator ImportedGrannyDrivesFacialMeshesAndEyeBonesInTheDemoScene()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Circuit_SuburbSquare");
            yield return null;
            var walker = Object.FindAnyObjectByType<ArcadeWalkerController>();
            walker.GetComponent<WalkerPresentation>().enabled = false;
            var animator = walker.GetComponentInChildren<Animator>();
            var face = animator.transform.Find("CHR_Granny_Face").GetComponent<SkinnedMeshRenderer>();
            Assert.That(face.sharedMesh.blendShapeCount, Is.EqualTo(10));
            Assert.That(face.sharedMaterial.name, Does.StartWith("MAT_GrannyV2_Palette"));
            var eye = Find(animator.transform, "Eye_L");
            Assert.That(eye, Is.Not.Null);
            animator.Play("Base Layer.Idle", 0, 0); animator.Update(0);
            var initial = eye.localRotation;
            animator.Play("Base Layer.Idle", 0, .25f); animator.Update(0);
            Assert.That(Quaternion.Angle(initial, eye.localRotation), Is.GreaterThan(3), "Eye gaze curve must animate the rig.");
            animator.Play("Base Layer.Idle", 0, 44f / 60); animator.Update(0);
            Assert.That(face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex("Blink_L")), Is.GreaterThan(95), "Idle blink must reach the face renderer.");
            animator.Play("Base Layer.Drive", 0, .5f); animator.Update(0);
            Assert.That(face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex("Determined")), Is.GreaterThan(75));
            animator.Play("Base Layer.Boost", 0, .5f); animator.Update(0);
            Assert.That(face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex("Delighted")), Is.GreaterThan(80));
            var bounds = face.bounds;
            Assert.That(bounds.size.magnitude, Is.InRange(.1f, 2f), "Facial skin bounds must remain near character scale.");
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            for (var i = 0; i < root.childCount; i++) { var found = Find(root.GetChild(i), name); if (found != null) return found; }
            return null;
        }
    }
}

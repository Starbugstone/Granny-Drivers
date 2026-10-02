using UnityEditor;
using UnityEngine;
using System;

namespace GrannyRacer.Editor
{
    /// <summary>The existing scene Animator lives on GrannyRig. Morph bindings are relative to it.</summary>
    public sealed class GrannyV3ModelPostprocessor : AssetPostprocessor
    {
        private void OnPostprocessAnimation(GameObject model, AnimationClip clip)
        {
            if (!assetPath.StartsWith("Assets/GrannyRacer/Art/Imported/Models/Granny/Granny_Walker@", StringComparison.Ordinal)) return;
            // Mesh-bearing FBXs retain an extra armature path compared with the previous
            // skeleton-only clips. The scene's existing Animator already occupies that root.
            foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            {
                if (binding.type != typeof(Transform)) continue;
                if (binding.path == "GrannyRig" || binding.path == "CHR_Granny" ||
                    binding.path == "CHR_Granny_Face" || binding.path == "VEH_Walker")
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    continue;
                }
                if (!binding.path.StartsWith("GrannyRig/", StringComparison.Ordinal)) continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var relative = binding; relative.path = binding.path.Substring("GrannyRig/".Length);
                AnimationUtility.SetEditorCurve(clip, binding, null);
                AnimationUtility.SetEditorCurve(clip, relative, curve);
            }
        }

        private void OnPostprocessModel(GameObject model)
        {
            if (assetPath != "Assets/GrannyRacer/Art/Imported/Models/Granny/Granny_Walker.fbx") return;
            var rig = model.transform.Find("GrannyRig");
            if (rig == null) return;
            var face = model.transform.Find("CHR_Granny_Face");
            if (face == null) return; // Earlier character exports have no facial renderer.
            foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                renderer.transform.SetParent(rig, true);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GrannyRacer.Walker;

namespace GrannyRacer.Editor
{
    public static class GrannyV3Importer
    {
        private const string Root = "Assets/GrannyRacer/Art/Imported/Models/Granny";
        private const string TexturePath = "Assets/GrannyRacer/Art/Textures/GrannyV2_Palette.png";
        private const string MaterialPath = "Assets/GrannyRacer/Art/Materials/MAT_GrannyV2_Palette.mat";
        private static readonly string[] Clips = { "Idle", "Drive", "TurnLeft", "TurnRight", "Boost", "HitReact", "AttackLeft", "AttackRight", "FacialReview" };

        [MenuItem("Granny Racer/Art/Import Reviewed Granny v3")]
        public static void ImportAndAudit()
        {
            const string staged = "Blender/Exports/Granny_v3";
            if (!File.Exists(staged + "/manifest.json")) throw new FileNotFoundException("Validated granny v3 staging is required.");
            File.Copy(staged + "/GrannyV2_Palette.png", TexturePath, true);
            File.Copy(staged + "/Granny_Walker.fbx", Root + "/Granny_Walker.fbx", true);
            foreach (var clip in Clips) File.Copy(staged + "/Granny_Walker@" + clip + ".fbx", Root + "/Granny_Walker@" + clip + ".fbx", true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var texture = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            texture.sRGBTexture = true; texture.mipmapEnabled = false; texture.filterMode = FilterMode.Point;
            texture.textureCompression = TextureImporterCompression.Uncompressed; texture.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, MaterialPath); }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
            material.SetColor("_BaseColor", Color.white); material.SetFloat("_Smoothness", .24f);
            EditorUtility.SetDirty(material);
            Configure(Root + "/Granny_Walker.fbx", null, false, material);
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(Root + "/Granny_Walker.fbx");
            if (avatar == null || !avatar.isValid) throw new InvalidDataException("Granny v3 Generic Avatar invalid.");
            foreach (var clip in Clips) Configure(Root + "/Granny_Walker@" + clip + ".fbx", avatar,
                clip != "HitReact" && !clip.StartsWith("Attack", StringComparison.Ordinal), material);
            AssetDatabase.SaveAssets();
            Audit();
        }

        private static void Configure(string path, Avatar avatar, bool loop, Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1; importer.useFileUnits = true; importer.bakeAxisConversion = false;
            importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
            importer.isReadable = false; importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.importBlendShapes = true; importer.importBlendShapeNormals = ModelImporterNormals.Calculate;
            importer.skinWeights = ModelImporterSkinWeights.Standard; importer.optimizeGameObjects = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = avatar == null ? ModelImporterAvatarSetup.CreateFromThisModel : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar; importer.motionNodeName = "Root";
            importer.importAnimation = avatar != null;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "MAT_GrannyV2_Palette"), material);
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "MAT_POC_Palette"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/GrannyRacer/Art/Materials/MAT_POC_Palette.mat"));
            if (avatar != null)
            {
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips) { clip.loopTime = loop; clip.lockRootPositionXZ = true; clip.lockRootHeightY = true; clip.lockRootRotation = true; }
                importer.clipAnimations = clips;
            }
            importer.SaveAndReimport();
        }

        public static void Audit()
        {
            var lines = new List<string>();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Granny_Walker.fbx");
            foreach (var mesh in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                lines.Add($"MESH {AnimationUtility.CalculateTransformPath(mesh.transform, model.transform)} vertices={mesh.sharedMesh.vertexCount} shapes={mesh.sharedMesh.blendShapeCount} bones={mesh.bones.Length} material={mesh.sharedMaterial.name}");
            foreach (var name in Clips)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(Root + "/Granny_Walker@" + name + ".fbx"))
                {
                    if (!(asset is AnimationClip clip) || clip.name.StartsWith("__preview__", StringComparison.Ordinal)) continue;
                    var shapes = 0;
                    foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (binding.type == typeof(Transform) && (binding.path.EndsWith("Eye_L", StringComparison.Ordinal) || binding.path.EndsWith("Head", StringComparison.Ordinal)))
                            lines.Add($"BONE {name} {binding.path} {binding.propertyName}");
                        if (binding.type != typeof(SkinnedMeshRenderer)) continue;
                        shapes++; lines.Add($"CURVE {name} {binding.path} {binding.propertyName}");
                    }
                    if (shapes < 14) throw new InvalidDataException("Missing facial curves in " + name);
                    lines.Add($"CLIP {name} duration={clip.length} facialCurves={shapes}");
                }
            }
            var scene = EditorSceneManager.OpenScene("Assets/GrannyRacer/Scenes/Tracks/Circuit_SuburbSquare.unity");
            foreach (var walker in UnityEngine.Object.FindObjectsByType<ArcadeWalkerController>())
                foreach (var animator in walker.GetComponentsInChildren<Animator>(true))
                {
                    lines.Add("SCENE ANIMATOR " + AnimationUtility.CalculateTransformPath(animator.transform, walker.transform));
                    lines.Add("AVATAR " + animator.avatar.name + " valid=" + animator.avatar.isValid);
                    foreach (var clip in animator.runtimeAnimatorController.animationClips)
                        lines.Add("CONTROLLER CLIP " + clip.name + " path=" + AssetDatabase.GetAssetPath(clip));
                    foreach (var mesh in walker.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        lines.Add("SCENE MESH " + AnimationUtility.CalculateTransformPath(mesh.transform, animator.transform));
                }
            Directory.CreateDirectory("Docs/Art");
            File.WriteAllLines("Docs/Art/GRANNY_V3_UNITY_IMPORT.txt", lines);
            Debug.Log("[GrannyRacer] Granny v3 imported; " + string.Join("\n", lines));
        }
    }
}

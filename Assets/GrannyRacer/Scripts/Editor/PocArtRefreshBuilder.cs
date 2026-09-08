using System;
using System.Collections.Generic;
using System.IO;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GrannyRacer.Editor
{
    public static class PocArtRefreshBuilder
    {
        private const string ModelRoot = "Assets/GrannyRacer/Art/Imported/Models/Granny";
        private const string KitRoot = "Assets/GrannyRacer/Art/Environment/RocketClub";
        private const string PalettePath = "Assets/GrannyRacer/Art/Textures/POC_Palette.png";
        private const string MaterialPath = "Assets/GrannyRacer/Art/Materials/MAT_POC_Palette.mat";
        private static readonly string[] LayerNames = { "Racer", "Track", "SoftObstacle", "HardObstacle",
            "DynamicHazard", "AttackHitbox", "AttackHurtbox", "Trigger", "Pickup", "ResetVolume", "Decoration" };

        [MenuItem("Granny Racer/POC/Import Blender Refresh and Build Scene")]
        public static void ImportAndBuild()
        {
            var staged = Path.Combine(Directory.GetCurrentDirectory(), "Blender/Exports/POC_Refresh");
            if (!File.Exists(Path.Combine(staged, "manifest.json")))
                throw new FileNotFoundException("Run the Blender POC refresh generator first.");
            EnsureFolder(KitRoot);
            EnsureFolder("Assets/GrannyRacer/Art/Textures");
            File.Copy(Path.Combine(staged, "POC_Palette.png"), PalettePath, true);
            var paths = new List<string>();
            foreach (var file in Directory.GetFiles(staged, "*.fbx"))
            {
                var name = Path.GetFileName(file);
                var target = name.StartsWith("Granny_Walker", StringComparison.Ordinal) ? ModelRoot + "/" + name
                    : name.StartsWith("Slipper_", StringComparison.Ordinal) ? ModelRoot + "/Slippers/" + name
                    : KitRoot + "/" + name;
                File.Copy(file, target, true); // Existing .meta identities remain untouched.
                paths.Add(target);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(PalettePath);
            textureImporter.sRGBTexture = true;
            textureImporter.mipmapEnabled = false;
            textureImporter.filterMode = FilterMode.Point;
            textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
            textureImporter.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .24f);
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            foreach (var path in paths)
            {
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1;
                importer.useFileUnits = true;
                importer.bakeAxisConversion = false;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.isReadable = false;
                importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "MAT_POC_Palette"), material);
                if (!Path.GetFileName(path).StartsWith("Granny_Walker", StringComparison.Ordinal))
                {
                    importer.animationType = ModelImporterAnimationType.None;
                    importer.importAnimation = false;
                }
                importer.SaveAndReimport();
            }
            ConfigureLayers();
            PocSceneBuilder.CreateCompletePoc();
            WriteImportAudit(paths);
            AssetDatabase.SaveAssets();
            Debug.Log("[POC ART] Import, scene build and mesh audit complete. Human art acceptance pending.");
        }

        public static void Decorate(TrackDefinition track)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(KitRoot + "/ENV_House.fbx") == null) return;
            var parent = new GameObject("Rocket Club Neighbourhood").transform;
            var points = track.Waypoints;
            var bounds = new Bounds(points[0].position, Vector3.zero);
            for (var i = 1; i < points.Length; i++) bounds.Encapsulate(points[i].position);
            var groundY = bounds.min.y - .98f;
            var travelled = 25f;
            var dashDistance = 0f;
            var index = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var next = points[(i + 1) % points.Length].position;
                var direction = next - points[i].position;
                travelled += direction.magnitude;
                dashDistance += direction.magnitude;
                var forward = new Vector3(direction.x, 0, direction.z).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                if (dashDistance >= 7f)
                {
                    Place("PRP_RoadDash", points[i].position + Vector3.up * .018f, forward, parent);
                    dashDistance = 0;
                }
                if (travelled < 25f) continue;
                travelled = 0f;
                var radial = points[i].position - bounds.center;
                var outward = Vector3.Dot(right, radial) >= 0 ? right : -right;
                var location = points[i].position + outward * (points[i].width * .5f + 11f);
                location.y = groundY;
                if (!ClearsRoad(location, track, 5f)) continue;
                Place(index % 3 == 0 ? "ENV_Tree" : "ENV_House", location, -outward, parent);
                var fence = location - outward * 3.8f;
                Place("PRP_Fence", fence, -outward, parent);
                Place(index % 2 == 0 ? "PRP_Bin" : "PRP_Bench", fence + forward * 2f, -outward, parent);
                var tree = location + forward * 6f;
                if (ClearsRoad(tree, track, 3f)) Place("ENV_Tree", tree, forward, parent);
                index++;
            }
            var startForward = (points[1].position - points[0].position).normalized;
            var gate = Place("PRP_StartGate", points[0].position, startForward, parent);
            gate.transform.localScale = new Vector3(Mathf.Max(1, points[0].width / 8f), 1, 1);
            var sign = new GameObject("Quiet Sunday sign");
            sign.transform.SetParent(gate.transform, false);
            sign.transform.localPosition = new Vector3(0, 4.55f, 0);
            var text = sign.AddComponent<TextMesh>();
            text.text = "QUIET SUNDAY";
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.fontSize = 80;
            text.characterSize = .095f;
            text.color = new Color(.97f,.88f,.65f);
            for (var i = 0; i < 8; i++)
                Place("PRP_Cone", points[0].position - startForward * (i * 2f + 3f)
                    + Vector3.Cross(Vector3.up, startForward) * (points[0].width * .5f - .3f), startForward, parent);
            ReplaceSegments();
            foreach (var collider in UnityEngine.Object.FindObjectsByType<Collider>())
            {
                var racer = collider.GetComponent<ArcadeWalkerController>();
                collider.gameObject.layer = LayerMask.NameToLayer(racer != null ? "Racer" : collider.isTrigger ? "Trigger" : "Track");
            }
            SetLayer(parent, LayerMask.NameToLayer("Decoration"));
            var ground = GameObject.Find("Safety Ground");
            if (ground != null) ground.GetComponent<Renderer>().sharedMaterial = SolidMaterial("MAT_POC_Grass", new Color(.23f,.38f,.13f));
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.63f,.79f,.89f);
            RenderSettings.ambientEquatorColor = new Color(.56f,.63f,.51f);
            RenderSettings.ambientGroundColor = new Color(.25f,.31f,.22f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.68f,.81f,.85f);
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 100;
            RenderSettings.fogEndDistance = 300;
            var camera = UnityEngine.Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = RenderSettings.fogColor;
                camera.farClipPlane = 350;
                var cameraData = camera.GetUniversalAdditionalCameraData();
                cameraData.renderPostProcessing = true;
                cameraData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
        }

        private static bool ClearsRoad(Vector3 candidate, TrackDefinition track, float margin)
        {
            var points = track.Waypoints;
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i].position;
                var b = points[(i + 1) % points.Length].position;
                a.y = candidate.y; b.y = candidate.y;
                var span = b - a;
                var t = Mathf.Clamp01(Vector3.Dot(candidate - a, span) / Mathf.Max(.001f, span.sqrMagnitude));
                if (Vector3.Distance(candidate, a + span * t) < points[i].width * .5f + margin) return false;
            }
            return true;
        }

        private static GameObject Place(string name, Vector3 position, Vector3 forward, Transform parent)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KitRoot + "/" + name + ".fbx");
            if (model == null) throw new FileNotFoundException("Missing Blender prop: " + name);
            // FBX roots carry Blender-to-Unity axis conversion. Apply placement outside
            // that transform; replacing its rotation makes houses and road marks stand sideways.
            var placement = new GameObject(name);
            placement.transform.SetParent(parent, false);
            placement.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            PrefabUtility.InstantiatePrefab(model, placement.transform);
            placement.isStatic = true;
            return placement;
        }

        private static void ReplaceSegments()
        {
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
            {
                var kerb = renderer.name.EndsWith("Kerb", StringComparison.Ordinal);
                var barrier = renderer.name.EndsWith("Barrier", StringComparison.Ordinal);
                if (!kerb && !barrier) continue;
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(KitRoot + (kerb ? "/PRP_Kerb.fbx" : "/PRP_Barrier.fbx"));
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                if (barrier) instance.transform.rotation = Quaternion.Euler(0,90,0) * instance.transform.rotation;
                var meshRenderer = instance.GetComponentInChildren<Renderer>();
                var bounds = meshRenderer.bounds;
                var fit = new Vector3(1 / bounds.size.x, 1 / bounds.size.y, 1 / bounds.size.z);
                // Normalize in a wrapper before placing under the existing collider transform.
                var wrapper = new GameObject("Blender visual").transform;
                instance.transform.SetParent(wrapper, true);
                wrapper.localScale = fit;
                wrapper.position = -Vector3.Scale(bounds.center, fit);
                var normalize = new GameObject("Normalized visual").transform;
                wrapper.SetParent(normalize, true);
                normalize.SetParent(renderer.transform, false);
                renderer.enabled = false;
            }
        }

        private static Material SolidMaterial(string name, Color color)
        {
            var path = "Assets/GrannyRacer/Art/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void ConfigureLayers()
        {
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            foreach (var name in LayerNames)
            {
                if (LayerMask.NameToLayer(name) >= 0) continue;
                var slot = -1;
                for (var i = 8; i < 32; i++)
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) { slot = i; break; }
                if (slot < 0) throw new InvalidOperationException("No free Unity layer for " + name);
                layers.GetArrayElementAtIndex(slot).stringValue = name;
            }
            tags.ApplyModifiedPropertiesWithoutUndo();
            var racer = LayerMask.NameToLayer("Racer");
            foreach (var name in new[] { "Trigger", "Pickup", "ResetVolume", "AttackHitbox", "AttackHurtbox", "Decoration" })
            {
                var layer = LayerMask.NameToLayer(name);
                for (var other = 0; other < 32; other++)
                    Physics.IgnoreLayerCollision(layer, other, other != racer || name.StartsWith("Attack", StringComparison.Ordinal) || name == "Decoration");
            }
            AssetDatabase.SaveAssets();
        }

        private static void SetLayer(Transform transform, int layer)
        {
            if (layer < 0) return;
            transform.gameObject.layer = layer;
            for (var i = 0; i < transform.childCount; i++) SetLayer(transform.GetChild(i), layer);
        }

        [Serializable] private sealed class MeshEntry { public string asset; public string mesh; public int vertices; public long triangles; public int submeshes; }
        [Serializable] private sealed class ImportAudit { public MeshEntry[] meshes; public bool humanReviewed; }
        private static void WriteImportAudit(List<string> paths)
        {
            var entries = new List<MeshEntry>();
            foreach (var path in paths)
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Mesh mesh)
                    {
                        long indices = 0;
                        for (var i = 0; i < mesh.subMeshCount; i++) indices += mesh.GetIndexCount(i);
                        entries.Add(new MeshEntry { asset = path, mesh = mesh.name, vertices = mesh.vertexCount, triangles = indices / 3, submeshes = mesh.subMeshCount });
                    }
            Directory.CreateDirectory("Docs/Art");
            File.WriteAllText("Docs/Art/POC_IMPORT_AUDIT.json", JsonUtility.ToJson(new ImportAudit { meshes = entries.ToArray(), humanReviewed = false }, true));
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }
}

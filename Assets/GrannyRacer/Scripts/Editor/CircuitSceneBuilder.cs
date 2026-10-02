using System;
using System.Collections.Generic;
using System.IO;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace GrannyRacer.Editor
{
    public static class CircuitSceneBuilder
    {
        public const string DefaultRecipePath = "Assets/GrannyRacer/Settings/Circuit_SuburbSquare.asset";
        public const string DefaultScenePath = "Assets/GrannyRacer/Scenes/Tracks/Circuit_SuburbSquare.unity";
        public const string KitRoot = "Assets/GrannyRacer/Art/Environment/SuburbCircuit";
        private const string ExistingKit = "Assets/GrannyRacer/Art/Environment/RocketClub";

        [MenuItem("Granny Racer/Circuit Builder/Create or Open Suburb Square")]
        public static void CreateDefault()
        {
            var recipe = AssetDatabase.LoadAssetAtPath<CircuitRecipe>(DefaultRecipePath);
            if (recipe == null)
            {
                ImportKit();
                recipe = ScriptableObject.CreateInstance<CircuitRecipe>();
                recipe.controlPoints = CircuitLayout.Square();
                recipe.palette = DefaultPalette();
                AssetDatabase.CreateAsset(recipe, DefaultRecipePath);
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = recipe;
            CircuitBuilderWindow.Open(recipe);
        }

        public static void BuildDefault()
        {
            CreateDefault();
            Build(AssetDatabase.LoadAssetAtPath<CircuitRecipe>(DefaultRecipePath), DefaultScenePath);
        }

        public static void ApplyArcadePresetAndBuild()
        {
            CreateDefault();
            var recipe = AssetDatabase.LoadAssetAtPath<CircuitRecipe>(DefaultRecipePath);
            recipe.controlPoints = CircuitLayout.Square();
            // The top-right driveway splits and merges inside a single ordered sector.
            recipe.controlPoints[8].checkpoint = false;
            recipe.drivewayShortcut = true;
            recipe.shortcutStartControl = 5; recipe.shortcutEndControl = 10;
            recipe.arcadeFeatures = true;
            EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssets();
            Build(recipe, DefaultScenePath);
        }

        public static string OutputFolder(CircuitRecipe recipe)
        {
            var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(recipe));
            if (string.IsNullOrEmpty(guid)) throw new ArgumentException("Save the recipe as a project asset before building.");
            return "Assets/GrannyRacer/Settings/Generated/Circuit_" + guid;
        }

        public static string ScenePath(CircuitRecipe recipe)
        {
            if (AssetDatabase.GetAssetPath(recipe) == DefaultRecipePath) return DefaultScenePath;
            if (AssetDatabase.GetAssetPath(recipe) == NeighborhoodTourBuilder.MapleRecipe) return NeighborhoodTourBuilder.MapleScene;
            if (AssetDatabase.GetAssetPath(recipe) == NeighborhoodTourBuilder.SchoolRecipe) return NeighborhoodTourBuilder.SchoolScene;
            return "Assets/GrannyRacer/Scenes/Tracks/" + Path.GetFileName(OutputFolder(recipe)) + ".unity";
        }

        public static void Build(CircuitRecipe recipe, string scenePath)
        {
            var points = CircuitLayout.Bake(recipe);
            ValidateAssets(recipe, points);
            var recipePath = AssetDatabase.GetAssetPath(recipe);
            var authoredExtras = recipe.extraAssets.Length;
            foreach (var layer in new[] { "Racer", "Track", "Trigger", "Decoration", "SoftObstacle" })
                if (LayerMask.NameToLayer(layer) < 0) throw new InvalidOperationException("Missing project layer " + layer + ". Run the existing POC import/setup command first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var folder = OutputFolder(recipe);
            EnsureFolder(folder);
            var trackPath = folder + "/Track.asset";
            var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(trackPath);
            if (track == null) { track = ScriptableObject.CreateInstance<TrackDefinition>(); AssetDatabase.CreateAsset(track, trackPath); }
            TrackLayoutBuilder.Resample(recipe.controlPoints, out var mapping, 3, 6);
            track.SetPrototypeData(points, recipe.laps,
                recipe.drivewayShortcut ? mapping[recipe.shortcutStartControl] : -1,
                recipe.drivewayShortcut ? mapping[recipe.shortcutEndControl] : -1, recipe.shortcutWidth);
            EditorUtility.SetDirty(track);
            PocSceneBuilder.CreateRaceScene(track, scenePath, folder + "/Road.asset", t => Decorate(recipe, t, folder));
            // Preserve every existing scene, while making the requested circuit the playable build entry.
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };
            foreach (var entry in EditorBuildSettings.scenes) if (entry.path != scenePath) scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/Art");
            var reportPath = scenePath == DefaultScenePath ? "Docs/Art/CIRCUIT_BUILD_REPORT.json"
                : "Docs/Art/" + Path.GetFileNameWithoutExtension(scenePath) + "_BUILD_REPORT.json";
            File.WriteAllText(reportPath, JsonUtility.ToJson(new BuildReport
            {
                recipe = recipePath, scene = scenePath, samples = points.Length,
                lapMetres = TrackLayoutBuilder.MeasureLapLength(points),
                tightestRadius = TrackLayoutBuilder.MeasureTightestCornerRadius(points),
                checkpoints = CountCheckpoints(points), authoredExtras = authoredExtras,
                humanReviewed = false
            }, true));
            Debug.Log($"[GrannyRacer] Circuit built: {scenePath}, {points.Length} samples, {TrackLayoutBuilder.MeasureLapLength(points):0} m. Human playtest pending.");
        }

        public static void ValidateAssets(CircuitRecipe recipe, TrackWaypoint[] points)
        {
            if (recipe.palette == null || recipe.palette.Length == 0) throw new ArgumentException("Add at least one house and one tree to the asset palette.");
            var house = false; var tree = false;
            foreach (var entry in recipe.palette)
            {
                if (entry == null) throw new ArgumentException("Remove empty palette rows.");
                ValidateModel(entry.prefab, entry.scale, entry.yaw);
                house |= entry.role == CircuitAssetRole.House; tree |= entry.role == CircuitAssetRole.Tree;
            }
            if (!house || !tree) throw new ArgumentException("The suburb palette needs House and Tree entries.");
            if (recipe.extraAssets == null) throw new ArgumentException("Extra assets list cannot be null.");
            for (var i = 0; i < recipe.extraAssets.Length; i++)
            {
                var extra = recipe.extraAssets[i];
                if (extra == null) throw new ArgumentException($"Extra asset {i} is empty.");
                ValidateModel(extra.prefab, extra.scale, extra.yaw);
                if (!CircuitLayout.Finite(extra.position.x) || !CircuitLayout.Finite(extra.position.y)
                    || !CircuitLayout.Finite(extra.position.z) || Mathf.Abs(extra.position.y) > .001f)
                    throw new ArgumentException($"Extra asset {i}: use a finite position at Y = 0.");
                var bounds = ModelBounds(extra.prefab, extra.scale, extra.yaw);
                var center = extra.position + new Vector3(bounds.center.x, 0, bounds.center.z);
                var radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
                if (!CircuitLayout.ClearsRoad(center, radius, points) || !CircuitLayout.ClearsShortcut(center, radius, recipe))
                    throw new ArgumentException($"Extra asset {i} ({extra.prefab.name}) overlaps the road/sidewalk clearance. Move it further into a lot.");
            }
        }

        private static void ValidateModel(GameObject model, float scale, float yaw)
        {
            if (model == null || !EditorUtility.IsPersistent(model) || model.GetComponentsInChildren<Renderer>(true).Length == 0)
                throw new ArgumentException("Every palette/extra row needs a project prefab or model containing a renderer.");
            if (!CircuitLayout.Finite(scale) || scale < .1f || scale > 10 || !CircuitLayout.Finite(yaw))
                throw new ArgumentException("Asset scale must be 0.1–10; yaw must be finite.");
        }

        public static Bounds ModelBounds(GameObject model, float scale, float yaw)
        {
            var wrapper = new GameObject("Circuit bounds probe") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                wrapper.transform.rotation = Quaternion.Euler(0, yaw, 0);
                wrapper.transform.localScale = Vector3.one * scale;
                PrefabUtility.InstantiatePrefab(model, wrapper.transform);
                return RendererBounds(wrapper);
            }
            finally { Object.DestroyImmediate(wrapper); }
        }

        public static Bounds RendererBounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void Decorate(CircuitRecipe recipe, TrackDefinition track, string folder)
        {
            var root = new GameObject("Suburb Lots").transform;
            var occupied = new List<Bounds>();
            foreach (var extra in recipe.extraAssets)
            {
                var placed = Place(extra.prefab, extra.position, extra.yaw, extra.scale, root);
                occupied.Add(RendererBounds(placed));
            }
            var random = new System.Random(recipe.seed);
            var points = track.Waypoints;
            var distance = 0f;
            var nextLot = recipe.lotSpacing * .5f;
            var nextDash = 2f;
            var dashModel = AssetDatabase.LoadAssetAtPath<GameObject>(ExistingKit + "/PRP_RoadDash.fbx");
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i].position; var b = points[(i + 1) % points.Length].position;
                var length = Vector3.Distance(a, b); var forward = (b - a).normalized;
                var right = Vector3.Cross(Vector3.up, forward);
                while (nextDash < distance + length)
                {
                    if (dashModel != null) Place(dashModel, a + forward * (nextDash - distance) + Vector3.up * .014f,
                        Quaternion.LookRotation(forward).eulerAngles.y, 1, root);
                    nextDash += 6;
                }
                while (nextLot < distance + length)
                {
                    var position = a + forward * (nextLot - distance);
                    var halfWidth = Mathf.Lerp(points[i].width, points[(i + 1) % points.Length].width, (nextLot - distance) / length) * .5f;
                    for (var side = -1; side <= 1; side += 2)
                    {
                        var outward = right * side;
                        var yaw = Quaternion.LookRotation(-outward).eulerAngles.y;
                        TryLot(recipe, CircuitAssetRole.House, position + outward * (halfWidth + recipe.houseSetback), yaw, track, random, root, occupied);
                        TryLot(recipe, CircuitAssetRole.Tree, position + outward * (halfWidth + recipe.houseSetback + 4) + forward * 8, yaw, track, random, root, occupied);
                        TryLot(recipe, CircuitAssetRole.Fence, position + outward * (halfWidth + 5.5f) + forward * 4, yaw, track, random, root, occupied);
                        TryLot(recipe, CircuitAssetRole.StreetFurniture, position + outward * (halfWidth + 5.2f) - forward * 4, yaw, track, random, root, occupied);
                    }
                    nextLot += recipe.lotSpacing;
                }
                distance += length;
            }
            var ground = GameObject.Find("Safety Ground");
            ground.transform.position = new Vector3(ground.transform.position.x, -.03f, ground.transform.position.z);
            ground.GetComponent<Renderer>().sharedMaterial = CircuitRoadBuilder.Material(folder, "Lawns", new Color(.28f,.43f,.14f));
            var boundsAll = RendererBounds(ground);
            foreach (var bounds in occupied) boundsAll.Encapsulate(bounds);
            var extent = Mathf.Max(boundsAll.size.x, boundsAll.size.z) + 40;
            ground.transform.position = new Vector3(boundsAll.center.x, -.03f, boundsAll.center.z);
            ground.transform.localScale = new Vector3(extent / 10, 1, extent / 10);
            var gateModel = AssetDatabase.LoadAssetAtPath<GameObject>(ExistingKit + "/PRP_StartGate.fbx");
            var startForward = (points[1].position - points[0].position).normalized;
            var gate = Place(gateModel, points[0].position, Quaternion.LookRotation(startForward).eulerAngles.y, 1, root);
            gate.transform.localScale = new Vector3((points[0].width + 7) / 8, 1, 1);
            var sign = new GameObject("Circuit name"); sign.transform.SetParent(root, false);
            sign.transform.SetPositionAndRotation(points[0].position + Vector3.up * 4.5f, Quaternion.LookRotation(startForward));
            var text = sign.AddComponent<TextMesh>(); text.text = recipe.circuitName; text.fontSize = 80;
            text.characterSize = .09f; text.anchor = TextAnchor.MiddleCenter; text.color = new Color(.98f,.87f,.60f);
            CircuitFeatureBuilder.Add(recipe, track, root, folder);
            if (recipe.neighborhoodDetails) NeighborhoodTourBuilder.AddNeighborhood(recipe, track, root, folder);
            NeighborhoodTourBuilder.AddTourAndAudio();
            // Reuse the project's established collision categories. Lot art is decoration.
            foreach (var collider in Object.FindObjectsByType<Collider>())
                collider.gameObject.layer = LayerMask.NameToLayer(collider.GetComponent<ArcadeWalkerController>() != null ? "Racer" : collider.isTrigger ? "Trigger" : "Track");
            SetLayer(root, LayerMask.NameToLayer("Decoration"));
            foreach (var pad in root.GetComponentsInChildren<TrackBoostPad>())
                pad.gameObject.layer = LayerMask.NameToLayer("Trigger");
            ground.layer = LayerMask.NameToLayer("SoftObstacle");
            Object.FindAnyObjectByType<ArcadeWalkerController>().ConfigureOffRoad(LayerMask.GetMask("SoftObstacle"));
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.64f,.78f,.88f);
            RenderSettings.ambientEquatorColor = new Color(.57f,.63f,.49f);
            RenderSettings.ambientGroundColor = new Color(.26f,.32f,.23f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.69f,.81f,.86f); RenderSettings.fogStartDistance = 130; RenderSettings.fogEndDistance = 380;
            var camera = UnityEngine.Camera.main; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RenderSettings.fogColor; camera.farClipPlane = 450;
        }

        private static void TryLot(CircuitRecipe recipe, CircuitAssetRole role, Vector3 position, float yaw,
            TrackDefinition track, System.Random random, Transform root, List<Bounds> occupied)
        {
            var candidates = new List<CircuitAssetEntry>();
            foreach (var entry in recipe.palette) if (entry.role == role) candidates.Add(entry);
            if (candidates.Count == 0) return;
            var chosen = candidates[random.Next(candidates.Count)];
            var bounds = ModelBounds(chosen.prefab, chosen.scale, yaw + chosen.yaw);
            bounds.center += position;
            var center = new Vector3(bounds.center.x, 0, bounds.center.z);
            var radius = new Vector2(bounds.extents.x, bounds.extents.z).magnitude;
            if (!CircuitLayout.ClearsRoad(center, radius, track.Waypoints) || !CircuitLayout.ClearsShortcut(center, radius, recipe)) return;
            var padded = bounds; padded.Expand(.6f);
            foreach (var other in occupied) if (padded.Intersects(other)) return;
            Place(chosen.prefab, position, yaw + chosen.yaw, chosen.scale, root);
            occupied.Add(bounds);
        }

        private static GameObject Place(GameObject prefab, Vector3 position, float yaw, float scale, Transform parent)
        {
            if (prefab == null) throw new InvalidOperationException("Missing required start gate model.");
            var wrapper = new GameObject(prefab.name + " Placement"); wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0)); wrapper.transform.localScale = Vector3.one * scale;
            PrefabUtility.InstantiatePrefab(prefab, wrapper.transform);
            var bounds = RendererBounds(wrapper);
            wrapper.transform.position += Vector3.up * (position.y - bounds.min.y);
            foreach (var collider in wrapper.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            var animated = wrapper.GetComponentsInChildren<Animation>(true).Length > 0;
            foreach (var child in wrapper.GetComponentsInChildren<Transform>()) child.gameObject.isStatic = !animated;
            foreach (var animation in wrapper.GetComponentsInChildren<Animation>(true))
            { animation.playAutomatically = true; animation.wrapMode = WrapMode.Loop; }
            return wrapper;
        }

        public static CircuitAssetEntry[] DefaultPalette()
        {
            var entries = new List<CircuitAssetEntry>();
            foreach (var path in new[] { KitRoot, ExistingKit })
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { path }))
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var name = model.name;
                if (name.Contains("Kerb") || name.Contains("Barrier") || name.Contains("Dash") || name.Contains("Gate")) continue;
                var role = name.Contains("House") || name.Contains("Garage") ? CircuitAssetRole.House
                    : name.Contains("Tree") ? CircuitAssetRole.Tree
                    : name.Contains("Fence") || name.Contains("Hedge") ? CircuitAssetRole.Fence : CircuitAssetRole.StreetFurniture;
                entries.Add(new CircuitAssetEntry { label = name, prefab = model, role = role });
            }
            entries.Sort((a,b) => string.CompareOrdinal(a.label,b.label));
            return entries.ToArray();
        }

        public static void ImportKit()
        {
            var staged = "Blender/Exports/SuburbCircuit";
            if (!File.Exists(staged + "/manifest.json")) throw new FileNotFoundException("Run Blender/Scripts/build_suburb_circuit_kit.py first.");
            EnsureFolder(KitRoot);
            foreach (var file in Directory.GetFiles(staged))
                if (file.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase) || file.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    File.Copy(file, KitRoot + "/" + Path.GetFileName(file), true);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var texturePath = KitRoot + "/SuburbPalette.png";
            var texture = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            texture.sRGBTexture = true; texture.mipmapEnabled = false; texture.filterMode = FilterMode.Point;
            texture.textureCompression = TextureImporterCompression.Uncompressed; texture.SaveAndReimport();
            var mat = CircuitRoadBuilder.Material(KitRoot, "MAT_SuburbPalette", Color.white);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath)); EditorUtility.SetDirty(mat);
            var audit = new List<ImportEntry>();
            foreach (var file in Directory.GetFiles(KitRoot, "*.fbx"))
            {
                var path = file.Replace('\\','/'); var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.globalScale = 1; importer.useFileUnits = true; importer.bakeAxisConversion = false;
                importer.importCameras = false; importer.importLights = false; importer.addCollider = false;
                importer.isReadable = false; importer.meshCompression = ModelImporterMeshCompression.Off;
                importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.animationType = ModelImporterAnimationType.None; importer.importAnimation = false;
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "MAT_SuburbPalette"), mat);
                importer.SaveAndReimport();
                long triangles = 0; var materials = 0;
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is Mesh mesh)
                    for (var i = 0; i < mesh.subMeshCount; i++) { triangles += (long)mesh.GetIndexCount(i) / 3; materials++; }
                audit.Add(new ImportEntry { asset = path, triangles = triangles, materials = materials,
                    dimensions = ModelBounds(AssetDatabase.LoadAssetAtPath<GameObject>(path), 1, 0).size });
            }
            Directory.CreateDirectory("Docs/Art");
            File.WriteAllText("Docs/Art/CIRCUIT_IMPORT_AUDIT.json", JsonUtility.ToJson(new ImportReport { assets = audit.ToArray() }, true));
            AssetDatabase.SaveAssets();
        }

        private static void SetLayer(Transform root, int layer) { root.gameObject.layer = layer; for (var i = 0; i < root.childCount; i++) SetLayer(root.GetChild(i), layer); }
        public static void EnsureFolder(string path) { if (AssetDatabase.IsValidFolder(path)) return; var slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0,slash)); AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1)); }
        private static int CountCheckpoints(TrackWaypoint[] points) { var count = 0; foreach (var point in points) if (point.checkpoint) count++; return count; }
        [Serializable] private sealed class BuildReport { public string recipe; public string scene; public int samples; public float lapMetres; public float tightestRadius; public int checkpoints; public int authoredExtras; public bool humanReviewed; }
        [Serializable] private sealed class ImportEntry { public string asset; public long triangles; public int materials; public Vector3 dimensions; }
        [Serializable] private sealed class ImportReport { public ImportEntry[] assets; public bool humanReviewed = false; }
    }
}

using System.IO;
using GrannyRacer.Camera;
using GrannyRacer.Input;
using GrannyRacer.Racing;
using GrannyRacer.UI;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class PocSceneBuilder
    {
        private const string ScenePath = "Assets/GrannyRacer/Scenes/Tracks/POC_QuietSunday.unity";
        private const string HandlingPath = "Assets/GrannyRacer/Settings/WalkerHandling_POC.asset";
        private const string HeatPath = "Assets/GrannyRacer/Settings/SlipperHeat_POC.asset";
        private const string TrackPath = "Assets/GrannyRacer/Settings/Track_QuietSunday_POC.asset";
        private const string WalkerPhysicsMaterialPath =
            "Assets/GrannyRacer/Settings/PM_Walker_Frictionless.physicsMaterial";

        [MenuItem("Granny Racer/POC/Create Complete Single-Racer POC")]
        public static void CreateCompletePoc()
        {
            EnsureFolder("Assets/GrannyRacer/Scenes/Tracks");
            EnsureFolder("Assets/GrannyRacer/Settings");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var handling = LoadOrCreate<WalkerHandlingSettings>(HandlingPath);
            var heat = LoadOrCreate<SlipperHeatSettings>(HeatPath);
            var track = LoadOrCreateTrack();

            CreateLight();
            CreateGround();

            var start = track.Waypoints[0].position + Vector3.up * 0.9f;
            var startForward = (track.Waypoints[1].position - track.Waypoints[0].position).normalized;
            var racer = CreateWalker(handling, heat, start, startForward);
            var input = racer.GetComponent<PlayerRacerInput>();
            var raceObject = new GameObject("Race Controller");
            var race = raceObject.AddComponent<RaceController>();

            var generatedTrack = new GameObject("Generated Track").transform;
            var checkpointCount = GreyboxTrackGenerator.Build(track, generatedTrack, race);
            race.Configure(new[] { racer }, new[] { start }, new[] { startForward }, 0, input,
                checkpointCount, track.Laps);
            CreateCamera(racer.transform);
            CreateHud(racer, race);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[POC] Created complete single-racer POC at {ScenePath}");
        }

        private static ArcadeWalkerController CreateWalker(WalkerHandlingSettings handling,
            SlipperHeatSettings heat, Vector3 position, Vector3 forward)
        {
            var root = new GameObject("Racer_PhysicsRoot");
            root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward, Vector3.up));
            var body = root.AddComponent<Rigidbody>();
            body.mass = 90f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.centerOfMass = new Vector3(0f, -0.35f, 0f);
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.25f, 1.35f, 1.5f);
            collider.sharedMaterial = LoadOrCreateWalkerPhysicsMaterial();
            root.AddComponent<PlayerRacerInput>();

            var visuals = new GameObject("Walker_Visuals").transform;
            visuals.SetParent(root.transform, false);
            CreatePart("Frame_Left", PrimitiveType.Cube, visuals, new Vector3(-0.55f, 0f, 0f), new Vector3(0.12f, 1.25f, 1.35f), new Color(0.15f, 0.65f, 0.85f));
            CreatePart("Frame_Right", PrimitiveType.Cube, visuals, new Vector3(0.55f, 0f, 0f), new Vector3(0.12f, 1.25f, 1.35f), new Color(0.15f, 0.65f, 0.85f));
            CreatePart("Frame_Handle", PrimitiveType.Cube, visuals, new Vector3(0f, 0.55f, 0.45f), new Vector3(1.2f, 0.12f, 0.12f), new Color(0.15f, 0.65f, 0.85f));
            CreatePart("Granny", PrimitiveType.Capsule, visuals, new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.85f, 0.7f), new Color(0.92f, 0.55f, 0.7f));
            CreatePart("Left Slipper", PrimitiveType.Sphere, visuals, new Vector3(-0.3f, -0.55f, 0.35f), new Vector3(0.32f, 0.15f, 0.55f), new Color(0.95f, 0.35f, 0.25f));
            CreatePart("Right Slipper", PrimitiveType.Sphere, visuals, new Vector3(0.3f, -0.55f, 0.35f), new Vector3(0.32f, 0.15f, 0.55f), new Color(0.95f, 0.35f, 0.25f));

            var controller = root.AddComponent<ArcadeWalkerController>();
            controller.Configure(handling, heat, visuals);
            return controller;
        }

        /// <summary>
        /// The walker models its own drive, braking and lateral grip. PhysX friction would
        /// double up on all three, and once <c>downforce</c> raises the normal impulse it
        /// cancels the drive force outright, leaving a walker that steers but will not move.
        /// </summary>
        private static PhysicsMaterial LoadOrCreateWalkerPhysicsMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(WalkerPhysicsMaterialPath);
            if (material == null)
            {
                // Values must be set before CreateAsset — writes afterwards are not persisted
                // by SaveAssets alone.
                material = new PhysicsMaterial("PM_Walker_Frictionless");
                ApplyFrictionless(material);
                AssetDatabase.CreateAsset(material, WalkerPhysicsMaterialPath);
                return material;
            }

            ApplyFrictionless(material);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static void ApplyFrictionless(PhysicsMaterial material)
        {
            material.staticFriction = 0f;
            material.dynamicFriction = 0f;
            material.frictionCombine = PhysicsMaterialCombine.Minimum;
            material.bounciness = 0f;
            material.bounceCombine = PhysicsMaterialCombine.Minimum;
        }

        private static TrackDefinition LoadOrCreateTrack()
        {
            var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(TrackPath);
            if (track == null)
            {
                track = ScriptableObject.CreateInstance<TrackDefinition>();
                AssetDatabase.CreateAsset(track, TrackPath);
            }

            if (track.Waypoints == null || track.Waypoints.Length < 3)
            {
                track.SetPrototypeData(CreatePrototypeWaypoints(), 3, 2, 4, 4.5f);
                EditorUtility.SetDirty(track);
                AssetDatabase.SaveAssets();
            }

            return track;
        }

        private static TrackWaypoint[] CreatePrototypeWaypoints()
        {
            return new[]
            {
                new TrackWaypoint(new Vector3(0f, 0.05f, -30f), 10f, true),
                new TrackWaypoint(new Vector3(24f, 0.25f, -24f), 10f, false),
                new TrackWaypoint(new Vector3(31f, 2.2f, 0f), 9f, true),
                new TrackWaypoint(new Vector3(23f, 1.1f, 25f), 7f, false),
                new TrackWaypoint(new Vector3(0f, 0.05f, 32f), 11f, true),
                new TrackWaypoint(new Vector3(-23f, 0.05f, 24f), 8f, false),
                new TrackWaypoint(new Vector3(-31f, 0.05f, 0f), 7f, true),
                new TrackWaypoint(new Vector3(-20f, 0.05f, -25f), 9f, false)
            };
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void CreateCamera(Transform target)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target.TransformPoint(new Vector3(0f, 4.5f, -7.5f));
            cameraObject.AddComponent<UnityEngine.Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<WalkerFollowCamera>().Configure(target);
        }

        private static void CreateHud(ArcadeWalkerController walker, RaceController race)
        {
            new GameObject("Debug HUD").AddComponent<DrivingDebugHud>().Configure(walker, race);
        }

        private static void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Safety Ground";
            ground.transform.position = new Vector3(0f, -0.2f, 0f);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
        }

        private static GameObject CreatePart(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Color colour)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            part.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = colour };
            return part;
        }

        private static void CreateLight()
        {
            var lightObject = new GameObject("Sun");
            lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

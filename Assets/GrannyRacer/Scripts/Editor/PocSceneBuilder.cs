using System.IO;
using GrannyRacer.Audio;
using GrannyRacer.Camera;
using GrannyRacer.Characters;
using GrannyRacer.Input;
using GrannyRacer.Racing;
using GrannyRacer.UI;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

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
        private const string GrannyModelRoot =
            "Assets/GrannyRacer/Art/Imported/Models/Granny";
        private const string GrannyModelPath = GrannyModelRoot + "/Granny_Walker.fbx";
        private const string GrannyAnimatorPath =
            "Assets/GrannyRacer/Settings/Generated/AC_GrannyWalker_POC.controller";
        private const string VfxMaterialRoot = "Assets/GrannyRacer/Settings/Generated";
        private static readonly Vector3 PocCameraOffset = new Vector3(0f, 2.4f, -4.2f);

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

            var controller = root.AddComponent<ArcadeWalkerController>();
            controller.Configure(handling, heat, visuals);
            CreateGrannyPresentation(root, visuals, collider, controller);
            return controller;
        }

        private static void CreateGrannyPresentation(GameObject root, Transform visuals,
            BoxCollider physicsCollider, ArcadeWalkerController controller)
        {
            ConfigureModelImporter(GrannyModelPath, false, null);
            var baseAvatar = LoadRequiredAvatar();
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@Idle.fbx", true, baseAvatar);
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@Drive.fbx", true, baseAvatar);
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@TurnLeft.fbx", true, baseAvatar);
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@TurnRight.fbx", true, baseAvatar);
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@Boost.fbx", true, baseAvatar);
            ConfigureModelImporter(GrannyModelRoot + "/Granny_Walker@HitReact.fbx", false, baseAvatar);

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GrannyModelPath);
            if (modelAsset == null)
            {
                throw new FileNotFoundException("The merged Granny walker FBX could not be loaded.",
                    GrannyModelPath);
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, visuals);
            model.name = "Granny_Walker_Model";
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            model.transform.localScale = Vector3.one;

            var importedAnimator = model.GetComponent<Animator>();
            var avatar = importedAnimator == null ? LoadRequiredAvatar() : importedAnimator.avatar;
            if (importedAnimator != null) Object.DestroyImmediate(importedAnimator);

            var rigRoot = FindDescendant(model.transform, "GrannyRig");
            if (rigRoot == null)
            {
                throw new InvalidDataException("The imported Granny model has no GrannyRig root.");
            }

            var animator = rigRoot.gameObject.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = LoadOrCreateGrannyAnimator();
            animator.applyRootMotion = false;

            var slipperSwapper = model.AddComponent<SlipperSwapper>();
            slipperSwapper.Configure(new[]
            {
                LoadRequiredModel(GrannyModelRoot + "/Slippers/Slipper_Classic.fbx"),
                LoadRequiredModel(GrannyModelRoot + "/Slippers/Slipper_Bunny.fbx"),
                LoadRequiredModel(GrannyModelRoot + "/Slippers/Slipper_Rocket.fbx")
            }, 0);

            AlignModelWithPhysics(model, visuals, root.transform, physicsCollider);

            var voice = root.AddComponent<GrannyVoice>();
            var presentation = root.AddComponent<WalkerPresentation>();
            presentation.Configure(controller, animator, voice, slipperSwapper);
            CreateWalkerVfx(root, model.transform, controller);
        }

        private static void CreateWalkerVfx(GameObject root, Transform model,
            ArcadeWalkerController controller)
        {
            var exhaustLeft = RequireDescendant(model, "FX_Exhaust_L");
            var exhaustRight = RequireDescendant(model, "FX_Exhaust_R");
            var slipperLeft = RequireDescendant(model, SlipperSwapper.LeftSocketName);
            var slipperRight = RequireDescendant(model, SlipperSwapper.RightSocketName);

            var softTexture = LoadOrCreateSoftParticleTexture();
            var flameMaterial = LoadOrCreateVfxMaterial("MAT_VFX_BoostFlame", new Color(1f, 0.23f, 0.02f, 0.9f), true, softTexture);
            var smokeMaterial = LoadOrCreateVfxMaterial("MAT_VFX_Smoke", new Color(0.25f, 0.27f, 0.3f, 0.48f), false, softTexture);
            // White and alpha-blended: the drift plume is tinted per charge tier at runtime.
            var driftMaterial = LoadOrCreateVfxMaterial("MAT_VFX_DriftSmoke", Color.white, false, softTexture);
            var skidMaterial = LoadOrCreateVfxMaterial("MAT_VFX_SkidMark", new Color(0.035f, 0.03f, 0.025f, 0.82f), false, softTexture);

            var flames = new[]
            {
                CreateParticles("Boost_Flame_L", exhaustLeft, flameMaterial, 0.22f, 6f, 0.28f, 60f, 0f),
                CreateParticles("Boost_Flame_R", exhaustRight, flameMaterial, 0.22f, 6f, 0.28f, 60f, 0f)
            };
            var rocketSmoke = new[]
            {
                CreateParticles("Boost_Smoke_L", exhaustLeft, smokeMaterial, 0.9f, 2.4f, 0.4f, 30f, -0.1f),
                CreateParticles("Boost_Smoke_R", exhaustRight, smokeMaterial, 0.9f, 2.4f, 0.4f, 30f, -0.1f)
            };
            var heatSmoke = new[]
            {
                CreateParticles("Slipper_Smoke_L", slipperLeft, smokeMaterial, 1.2f, 0.9f, 0.3f, 18f, -0.25f),
                CreateParticles("Slipper_Smoke_R", slipperRight, smokeMaterial, 1.2f, 0.9f, 0.3f, 18f, -0.25f)
            };
            var driftSmoke = new[]
            {
                CreateParticles("Drift_Smoke_L", slipperLeft, driftMaterial, 0.7f, 1.6f, 0.34f, 55f, -0.35f),
                CreateParticles("Drift_Smoke_R", slipperRight, driftMaterial, 0.7f, 1.6f, 0.34f, 55f, -0.35f)
            };

            // Parented to the racer root, not to a slipper: the marks are projected onto the
            // road each frame, and a foot leaving the ground would otherwise draw in mid-air.
            var marks = new[]
            {
                CreateSkidMark("Skid_Mark_L", root.transform, skidMaterial),
                CreateSkidMark("Skid_Mark_R", root.transform, skidMaterial)
            };

            root.AddComponent<WalkerVfxPresentation>().Configure(controller, flames, rocketSmoke,
                heatSmoke, driftSmoke, marks, new[] { slipperLeft, slipperRight });
        }

        private static Transform RequireDescendant(Transform root, string childName)
        {
            var child = FindDescendant(root, childName);
            if (child == null)
            {
                throw new InvalidDataException($"The Granny rig is missing required VFX socket '{childName}'.");
            }

            return child;
        }

        private static ParticleSystem CreateParticles(string name, Transform parent,
            Material material, float lifetime, float speed, float size, float rate, float gravity)
        {
            var effect = new GameObject(name);
            effect.transform.SetParent(parent, false);
            // Left at 1 deliberately. The rig's bones carry a 100x scale, but a particle system
            // set to Local scaling reads only its own transform, so sizes and speeds below are
            // literal metres. Compensating for the parent here is what made the first pass
            // invisible: 0.13 m of flame came out at 1.3 mm.
            effect.transform.localScale = Vector3.one;

            var particles = effect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.maxParticles = 220;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            // Negative gravity floats the smoke up regardless of how the socket bone is
            // oriented on any given animation frame.
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = particles.emission;
            emission.rateOverTime = rate;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = gravity < 0f ? 22f : 9f;
            shape.radius = gravity < 0f ? 0.07f : 0.04f;

            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
                new Gradient
                {
                    alphaKeys = new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    },
                    colorKeys = new[]
                    {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(Color.white, 1f)
                    }
                });

            var renderer = effect.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        private static TrailRenderer CreateSkidMark(string name, Transform parent, Material material)
        {
            var effect = new GameObject(name);
            effect.transform.SetParent(parent, false);
            effect.transform.localScale = Vector3.one;
            var trail = effect.AddComponent<TrailRenderer>();
            // The brief: marks linger for four seconds, then go.
            trail.time = 4f;
            trail.startWidth = 0.14f;
            trail.endWidth = 0.1f;
            trail.minVertexDistance = 0.05f;
            trail.autodestruct = false;
            trail.alignment = LineAlignment.TransformZ;
            trail.textureMode = LineTextureMode.Stretch;
            trail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.85f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                }
            };
            trail.sharedMaterial = material;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;
            return trail;
        }

        /// <summary>
        /// A soft round sprite. Without it the URP particle shader draws untextured quads, so
        /// smoke and flame read as hard white squares.
        /// </summary>
        private static Texture2D LoadOrCreateSoftParticleTexture()
        {
            EnsureFolder(VfxMaterialRoot);
            const string path = VfxMaterialRoot + "/TEX_VFX_SoftParticle.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "TEX_VFX_SoftParticle",
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[size * size];
            var centre = (size - 1) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x - centre) / centre;
                    var dy = (y - centre) / centre;
                    var falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    var alpha = falloff * falloff * (3f - 2f * falloff);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            AssetDatabase.SaveAssets();
            return texture;
        }

        private static Material LoadOrCreateVfxMaterial(string name, Color color, bool additive,
            Texture2D baseMap)
        {
            EnsureFolder(VfxMaterialRoot);
            var path = VfxMaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidDataException("A URP unlit shader is required for POC VFX.");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (baseMap != null)
            {
                if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", baseMap);
                if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", baseMap);
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            return material;
        }

        private static GameObject LoadRequiredModel(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new FileNotFoundException("Required model is missing.", path);
            return model;
        }

        private static Avatar LoadRequiredAvatar()
        {
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(GrannyModelPath);
            if (avatar == null)
            {
                throw new FileNotFoundException(
                    "The base Granny walker FBX did not import a Generic Avatar.", GrannyModelPath);
            }

            return avatar;
        }

        private static RuntimeAnimatorController LoadOrCreateGrannyAnimator()
        {
            EnsureFolder("Assets/GrannyRacer/Settings/Generated");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(GrannyAnimatorPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(GrannyAnimatorPath);
            }

            var stateMachine = controller.layers[0].stateMachine;
            var idle = SetStateMotion(stateMachine, "Idle", LoadAnimationClip("Idle"));
            SetStateMotion(stateMachine, "Drive", LoadAnimationClip("Drive"));
            SetStateMotion(stateMachine, "TurnLeft", LoadAnimationClip("TurnLeft"));
            SetStateMotion(stateMachine, "TurnRight", LoadAnimationClip("TurnRight"));
            SetStateMotion(stateMachine, "Boost", LoadAnimationClip("Boost"));
            SetStateMotion(stateMachine, "HitReact", LoadAnimationClip("HitReact"));
            stateMachine.defaultState = idle;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static AnimatorState SetStateMotion(AnimatorStateMachine stateMachine,
            string stateName, AnimationClip clip)
        {
            var states = stateMachine.states;
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i].state.name != stateName) continue;
                states[i].state.motion = clip;
                return states[i].state;
            }

            var state = stateMachine.AddState(stateName);
            state.motion = clip;
            return state;
        }

        private static AnimationClip LoadAnimationClip(string clipName)
        {
            var path = GrannyModelRoot + "/Granny_Walker@" + clipName + ".fbx";
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            throw new FileNotFoundException($"No animation clip was imported from {path}.", path);
        }

        private static void ConfigureModelImporter(string path, bool loop, Avatar sourceAvatar)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Model importer is missing.", path);

            importer.globalScale = 1f;
            importer.useFileUnits = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.bakeAxisConversion = false;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = sourceAvatar == null
                ? ModelImporterAvatarSetup.CreateFromThisModel
                : ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.removeConstantScaleCurves = sourceAvatar != null;
            importer.motionNodeName = "Root";

            if (importer.importAnimation)
            {
                var clips = importer.defaultClipAnimations;
                for (var i = 0; i < clips.Length; i++) clips[i].loopTime = loop;
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
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

        private static void AlignModelWithPhysics(GameObject model, Transform visuals,
            Transform physicsRoot, BoxCollider physicsCollider)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            var colliderBottom = physicsCollider.center.y - physicsCollider.size.y * 0.5f;
            var targetBottom = physicsRoot.TransformPoint(0f, colliderBottom, 0f).y;
            visuals.position += Vector3.up * (targetBottom - bounds.min.y + 0.02f);
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
            cameraObject.transform.position = target.TransformPoint(PocCameraOffset);
            cameraObject.AddComponent<UnityEngine.Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<WalkerFollowCamera>().Configure(target, PocCameraOffset);
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

using System.IO;
using GrannyRacer.Racing;
using GrannyRacer.Walker;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class ArcadeDemoTuning
    {
        [MenuItem("Granny Racer/Apply Arcade Speed and Repair Driveway Meshes")]
        public static void Apply()
        {
            var handling = AssetDatabase.LoadAssetAtPath<WalkerHandlingSettings>("Assets/GrannyRacer/Settings/WalkerHandling_POC.asset");
            handling.acceleration = 36; handling.braking = 42;
            handling.maximumSpeed = 24; handling.boostMaximumSpeed = 35; handling.boostAcceleration = 52;
            handling.lateralGrip = 12; handling.boostGripScale = .8f;
            handling.steeringDegreesPerSecond = 155; handling.steeringFalloffSpeed = 35; handling.topSpeedSteeringScale = .7f;
            handling.boostReleaseDeceleration = 8;
            EditorUtility.SetDirty(handling);
            var heat = AssetDatabase.LoadAssetAtPath<SlipperHeatSettings>("Assets/GrannyRacer/Settings/SlipperHeat_POC.asset");
            heat.heatPerSecond = .24f; heat.coolingDelay = .2f; heat.coolingPerSecond = .32f;
            heat.replacementDuration = 2.4f; heat.burnoutSpeedScale = .55f;
            heat.burnoutDeceleration = 18; heat.criticalGripScale = .88f;
            EditorUtility.SetDirty(heat);

            // Update existing mesh assets in place. Scenes and their authored props stay intact;
            // both the renderer and collider already reference these exact mesh GUIDs.
            var repaired = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:TrackDefinition", new[] { "Assets/GrannyRacer/Settings/Generated" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var track = AssetDatabase.LoadAssetAtPath<TrackDefinition>(path);
                var meshPath = Path.GetDirectoryName(path).Replace('\\', '/') + "/Driveway.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null || track.ShortcutStartWaypoint < 0) continue;
                var generated = DrivewayGeometry.Build(track);
                EditorUtility.CopySerialized(generated, mesh);
                Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(mesh); repaired++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ARCADE PASS] Cruise 24 m/s, boost 35 m/s; repaired {repaired} driveway mesh assets without rebuilding scenes.");
        }
    }
}

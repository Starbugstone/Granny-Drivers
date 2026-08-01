using System;
using System.Collections.Generic;
using GrannyRacer.Racing;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class GreyboxTrackGenerator
    {
        private const string GeneratedFolder = "Assets/GrannyRacer/Settings/Generated";
        private const string RoadMeshPath = GeneratedFolder + "/QuietSunday_Road.asset";
        private const string RoadMaterialPath = "Assets/GrannyRacer/Art/Materials/MAT_POC_Road.mat";
        private const string KerbMaterialPath = "Assets/GrannyRacer/Art/Materials/MAT_POC_Kerb.mat";
        private const string ShortcutMaterialPath = "Assets/GrannyRacer/Art/Materials/MAT_POC_Shortcut.mat";

        public static int Build(TrackDefinition track, Transform parent, RaceController race)
        {
            if (track == null || track.Waypoints == null || track.Waypoints.Length < 3)
            {
                throw new InvalidOperationException("A closed POC track needs at least three waypoints.");
            }

            EnsureFolder(GeneratedFolder);
            EnsureFolder("Assets/GrannyRacer/Art/Materials");
            var roadMaterial = GetOrCreateMaterial(RoadMaterialPath, new Color(0.19f, 0.22f, 0.25f));
            var kerbMaterial = GetOrCreateMaterial(KerbMaterialPath, new Color(0.92f, 0.82f, 0.2f));
            var shortcutMaterial = GetOrCreateMaterial(ShortcutMaterialPath, new Color(0.34f, 0.29f, 0.22f));

            CreateRoad(track, parent, roadMaterial);
            CreateKerbs(track, parent, kerbMaterial);
            CreateBarriers(track, parent, kerbMaterial);
            CreateShortcut(track, parent, shortcutMaterial);
            CreateSpawnGrid(track, parent);
            return CreateCheckpoints(track, parent, race);
        }

        /// <summary>
        /// Builds the closed road ribbon for the given waypoints. Triangles are wound so the
        /// surface normal points up.
        /// </summary>
        public static void BuildRoadGeometry(TrackWaypoint[] points, out Vector3[] vertices,
            out int[] triangles)
        {
            vertices = new Vector3[points.Length * 2];
            triangles = new int[points.Length * 6];
            for (var i = 0; i < points.Length; i++)
            {
                var previous = points[(i - 1 + points.Length) % points.Length].position;
                var next = points[(i + 1) % points.Length].position;
                var direction = next - previous;
                direction.y = 0f;
                direction.Normalize();
                var right = Vector3.Cross(Vector3.up, direction);
                vertices[i * 2] = points[i].position - right * (points[i].width * 0.5f);
                vertices[i * 2 + 1] = points[i].position + right * (points[i].width * 0.5f);

                var nextIndex = (i + 1) % points.Length;
                var triangle = i * 6;
                // Wound so the surface normal points up. A downward-facing road renders
                // inside-out and its MeshCollider is only solid from below, which drops the
                // walker through the track.
                triangles[triangle] = i * 2;
                triangles[triangle + 1] = nextIndex * 2;
                triangles[triangle + 2] = nextIndex * 2 + 1;
                triangles[triangle + 3] = i * 2;
                triangles[triangle + 4] = nextIndex * 2 + 1;
                triangles[triangle + 5] = i * 2 + 1;
            }
        }

        private static void CreateRoad(TrackDefinition track, Transform parent, Material material)
        {
            BuildRoadGeometry(track.Waypoints, out var vertices, out var triangles);

            var generated = new Mesh { name = "QuietSunday_Road" };
            generated.vertices = vertices;
            generated.triangles = triangles;
            generated.RecalculateNormals();
            generated.RecalculateBounds();

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(RoadMeshPath);
            if (mesh == null)
            {
                AssetDatabase.CreateAsset(generated, RoadMeshPath);
                mesh = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, mesh);
                UnityEngine.Object.DestroyImmediate(generated);
            }

            var road = new GameObject("Generated Road");
            road.transform.SetParent(parent, false);
            road.AddComponent<MeshFilter>().sharedMesh = mesh;
            road.AddComponent<MeshRenderer>().sharedMaterial = material;
            road.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateKerbs(TrackDefinition track, Transform parent, Material material)
        {
            var points = track.Waypoints;
            for (var i = 0; i < points.Length; i++)
            {
                var nextIndex = (i + 1) % points.Length;
                var start = points[i].position;
                var end = points[nextIndex].position;
                var direction = end - start;
                var length = direction.magnitude;
                if (length < 0.01f) continue;
                var horizontal = new Vector3(direction.x, 0f, direction.z).normalized;
                var right = Vector3.Cross(Vector3.up, horizontal);
                var width = (points[i].width + points[nextIndex].width) * 0.25f + 0.28f;
                CreateSegment("Outer Kerb", start + right * width, end + right * width,
                    new Vector3(0.48f, 0.22f, length), material, parent);
                CreateSegment("Inner Kerb", start - right * width, end - right * width,
                    new Vector3(0.48f, 0.22f, length), material, parent);
            }
        }

        private static void CreateShortcut(TrackDefinition track, Transform parent, Material material)
        {
            var points = track.Waypoints;
            if (track.ShortcutStartWaypoint < 0 || track.ShortcutStartWaypoint >= points.Length
                || track.ShortcutEndWaypoint < 0 || track.ShortcutEndWaypoint >= points.Length) return;
            var start = points[track.ShortcutStartWaypoint].position;
            var end = points[track.ShortcutEndWaypoint].position;
            var length = Vector3.Distance(start, end);
            CreateSegment("Risky Driveway Shortcut", start, end,
                new Vector3(track.ShortcutWidth, 0.12f, length), material, parent);
        }

        private static void CreateBarriers(TrackDefinition track, Transform parent, Material material)
        {
            var points = track.Waypoints;
            for (var i = 0; i < points.Length; i++)
            {
                var nextIndex = (i + 1) % points.Length;
                var start = points[i].position;
                var end = points[nextIndex].position;
                var direction = end - start;
                var length = direction.magnitude;
                if (length < 0.01f) continue;
                var horizontal = new Vector3(direction.x, 0f, direction.z).normalized;
                var right = Vector3.Cross(Vector3.up, horizontal);
                var width = (points[i].width + points[nextIndex].width) * 0.25f + 0.8f;
                CreateSegment("Outer Barrier", start + right * width, end + right * width,
                    new Vector3(0.22f, 1.1f, length), material, parent);
                var innerStart = start - right * width;
                var innerEnd = end - right * width;
                if (!BlocksShortcut(track, innerStart, innerEnd))
                {
                    CreateSegment("Inner Barrier", innerStart, innerEnd,
                        new Vector3(0.22f, 1.1f, length), material, parent);
                }
            }
        }

        /// <summary>
        /// True where the inner barrier has to be left out because the shortcut runs through it.
        /// </summary>
        /// <remarks>
        /// Measured against the driveway's own geometry rather than by waypoint index. Two
        /// reasons: waypoints are generated at whatever spacing the corner's curvature
        /// demands, so an index match can open a gap only two metres wide, and the driveway
        /// peels away from the road gradually — it is still inside the barrier line for a good
        /// ten metres past the mouth, which a fixed radius around the mouth does not cover.
        /// </remarks>
        private static bool BlocksShortcut(TrackDefinition track, Vector3 barrierStart,
            Vector3 barrierEnd)
        {
            var points = track.Waypoints;
            if (track.ShortcutStartWaypoint < 0 || track.ShortcutStartWaypoint >= points.Length
                || track.ShortcutEndWaypoint < 0 || track.ShortcutEndWaypoint >= points.Length) return false;

            var start = points[track.ShortcutStartWaypoint].position;
            var end = points[track.ShortcutEndWaypoint].position;
            // Half the driveway plus a metre of verge. Both ends of the barrier are tested,
            // not its midpoint: a long barrier alongside the mouth can clear the driveway in
            // the middle and still have one end standing in it.
            var clearance = track.ShortcutWidth * 0.5f + 1f;
            return DistanceToSegment(barrierStart, start, end) < clearance
                || DistanceToSegment(barrierEnd, start, end) < clearance;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            var span = end - start;
            var lengthSquared = span.sqrMagnitude;
            if (lengthSquared < 1e-6f) return Vector3.Distance(point, start);
            var t = Mathf.Clamp01(Vector3.Dot(point - start, span) / lengthSquared);
            return Vector3.Distance(point, start + span * t);
        }

        private static void CreateSpawnGrid(TrackDefinition track, Transform parent)
        {
            var start = track.Waypoints[0].position;
            var forward = (track.Waypoints[1].position - start).normalized;
            var spawn = new GameObject("Spawn Grid 0");
            spawn.transform.SetParent(parent, false);
            spawn.transform.SetPositionAndRotation(start + Vector3.up * 0.9f,
                Quaternion.LookRotation(forward, Vector3.up));
        }

        private static int CreateCheckpoints(TrackDefinition track, Transform parent, RaceController race)
        {
            var checkpointWaypoints = new List<int>();
            for (var i = 0; i < track.Waypoints.Length; i++)
            {
                if (track.Waypoints[i].checkpoint) checkpointWaypoints.Add(i);
            }

            for (var checkpointIndex = 0; checkpointIndex < checkpointWaypoints.Count; checkpointIndex++)
            {
                var waypointIndex = checkpointWaypoints[checkpointIndex];
                var point = track.Waypoints[waypointIndex];
                var next = track.Waypoints[(waypointIndex + 1) % track.Waypoints.Length].position;
                var direction = next - point.position;
                direction.y = 0f;
                var checkpointName = checkpointIndex == 0
                    ? "Lap Trigger (Checkpoint 0)"
                    : $"Checkpoint {checkpointIndex}";
                var checkpoint = new GameObject(checkpointName);
                checkpoint.transform.SetParent(parent, false);
                checkpoint.transform.position = point.position + Vector3.up;
                checkpoint.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                var trigger = checkpoint.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(point.width, 2.5f, 1.2f);
                checkpoint.AddComponent<RaceCheckpoint>().Configure(checkpointIndex, race);
                var reset = new GameObject($"Reset Pose {checkpointIndex}");
                reset.transform.SetParent(checkpoint.transform, false);
            }

            return checkpointWaypoints.Count;
        }

        private static void CreateSegment(string name, Vector3 start, Vector3 end, Vector3 scale,
            Material material, Transform parent)
        {
            var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.SetParent(parent, false);
            segment.transform.position = (start + end) * 0.5f + Vector3.up * (scale.y * 0.5f);
            var direction = end - start;
            segment.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            segment.transform.localScale = scale;
            segment.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material GetOrCreateMaterial(string path, Color colour)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { color = colour };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}

using System.IO;
using GrannyRacer.Racing;
using UnityEditor;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class CircuitRoadBuilder
    {
        public static void BuildEdges(TrackDefinition track, Transform parent, string roadPath)
        {
            var folder = Path.GetDirectoryName(roadPath).Replace('\\', '/');
            var concrete = Material(folder, "Concrete", new Color(.72f, .70f, .62f));
            var yellow = Material(folder, "RouteYellow", new Color(.97f, .64f, .08f));
            var teal = Material(folder, "RouteTeal", new Color(.04f, .37f, .34f));
            // Every strip uses the same edge vertices as the asphalt, including the closing seam.
            for (var side = -1; side <= 1; side += 2)
            {
                Strip(track.Waypoints, side, 0, .35f, 0, .13f, "CurbRamp" + side, concrete, folder, parent);
                Strip(track.Waypoints, side, .35f, 3.2f, .13f, .13f, "Sidewalk" + side, concrete, folder, parent);
                Strip(track.Waypoints, side, 3.2f, 3.5f, .13f, .13f, "BoundaryTop" + side, teal, folder, parent);
                Strip(track.Waypoints, side, 3.25f, 3.25f, .13f, 1.05f, "Boundary" + side, teal, folder, parent, true, track);
                Strip(track.Waypoints, side, -.2f, -.07f, .012f, .012f, "EdgePaint" + side, yellow, folder, parent, false);
            }
        }

        private static void Strip(TrackWaypoint[] points, int side, float near, float far, float nearY,
            float farY, string name, Material material, string folder, Transform parent, bool collision = true, TrackDefinition shortcut = null)
        {
            GreyboxTrackGenerator.BuildRoadGeometry(points, out var road, out _);
            var vertices = new Vector3[points.Length * 2];
            var normals = new Vector3[vertices.Length];
            var indices = new int[points.Length * 12];
            var written = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var right = (road[i * 2 + 1] - road[i * 2]).normalized * side;
                var edge = road[i * 2 + (side > 0 ? 1 : 0)];
                vertices[2 * i] = edge + right * near + Vector3.up * nearY;
                vertices[2 * i + 1] = edge + right * far + Vector3.up * farY;
                var normal = Mathf.Abs(far - near) < .001f ? -right
                    : (Vector3.up * (far - near) - right * (farY - nearY)).normalized;
                normals[2 * i] = normal; normals[2 * i + 1] = normal;
                var n = (i + 1) % points.Length * 2;
                if (shortcut != null && shortcut.ShortcutStartWaypoint >= 0)
                {
                    var a = points[shortcut.ShortcutStartWaypoint].position;
                    var b = points[shortcut.ShortcutEndWaypoint].position;
                    var edgeNext = road[n + (side > 0 ? 1 : 0)];
                    var candidate = (edge + edgeNext) * .5f + right * near;
                    if (CircuitLayout.DistanceToSegment(candidate,a,b) < shortcut.ShortcutWidth * .5f + 2) continue;
                }
                var t = written;
                indices[t] = 2 * i; indices[t + 1] = n; indices[t + 2] = n + 1;
                indices[t + 3] = 2 * i; indices[t + 4] = n + 1; indices[t + 5] = 2 * i + 1;
                // Two-sided strips support inside/outside contact on the vertical boundary.
                for (var j = 0; j < 6; j += 3)
                { indices[t + 6 + j] = indices[t + j]; indices[t + 7 + j] = indices[t + 2 + j]; indices[t + 8 + j] = indices[t + 1 + j]; }
                written += 12;
            }
            System.Array.Resize(ref indices, written);
            var mesh = new Mesh { name = name, vertices = vertices, triangles = indices };
            mesh.normals = normals; mesh.RecalculateBounds();
            var path = folder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (collision) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
        }

        public static void BuildShortcut(TrackDefinition track, Transform parent, string roadPath)
        {
            if (track.ShortcutStartWaypoint < 0) return;
            var folder=Path.GetDirectoryName(roadPath).Replace('\\','/');
            var generated = DrivewayGeometry.Build(track);
            var path=folder+"/Driveway.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh==null) { AssetDatabase.CreateAsset(generated,path);mesh=generated; }
            else { EditorUtility.CopySerialized(generated,mesh);Object.DestroyImmediate(generated); }
            var go=new GameObject("Garden driveway");go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial=Material(folder,"Driveway",new Color(.55f,.40f,.23f));
            go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;
        }

        public static Material Material(string folder, string name, Color color)
        {
            var path = folder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color, enableInstancing = true };
            mat.SetFloat("_Smoothness", .15f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }
    }
}

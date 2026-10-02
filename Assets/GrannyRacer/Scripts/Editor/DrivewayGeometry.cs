using System.Collections.Generic;
using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.Editor
{
    /// <summary>Subtract the asphalt footprint before triangulating a garden path.</summary>
    public static class DrivewayGeometry
    {
        public static Mesh Build(TrackDefinition track)
        {
            GreyboxTrackGenerator.BuildRoadGeometry(track.Waypoints, out var road, out var roadIndices);
            var a = track.Waypoints[track.ShortcutStartWaypoint].position;
            var b = track.Waypoints[track.ShortcutEndWaypoint].position;
            var direction = (b - a).normalized;
            var right = Vector3.Cross(Vector3.up, direction) * track.ShortcutWidth * .5f;
            var steps = Mathf.CeilToInt(Vector3.Distance(a, b) / .5f);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < steps; i++)
            {
                var start = Vector3.Lerp(a, b, (float)i / steps);
                var end = Vector3.Lerp(a, b, (float)(i + 1) / steps);
                var pieces = new List<List<Vector3>> { new List<Vector3> { start-right, end-right, end+right, start+right } };
                for (var t = 0; t < roadIndices.Length && pieces.Count > 0; t += 3)
                {
                    var next = new List<List<Vector3>>();
                    foreach (var polygon in pieces)
                        Subtract(polygon, road[roadIndices[t]], road[roadIndices[t+1]], road[roadIndices[t+2]], next);
                    pieces = next;
                }
                foreach (var polygon in pieces)
                {
                    var index = vertices.Count;
                    foreach (var point in polygon)
                    {
                        var distance = float.PositiveInfinity;
                        for (var j = 0; j < road.Length; j += 2)
                        {
                            var n = (j + 2) % road.Length;
                            distance = Mathf.Min(distance, CircuitLayout.DistanceToSegment(point, road[j], road[n]),
                                CircuitLayout.DistanceToSegment(point, road[j+1], road[n+1]));
                        }
                        vertices.Add(new Vector3(point.x, Mathf.Clamp01(distance / .35f) * .13f + .001f, point.z));
                    }
                    for (var j = 1; j < polygon.Count - 1; j++)
                    {
                        if (Mathf.Abs(Cross(polygon[j] - polygon[0], polygon[j+1] - polygon[0])) < .000001f) continue;
                        triangles.Add(index); triangles.Add(index+j); triangles.Add(index+j+1);
                    }
                }
            }
            var mesh = new Mesh { name = "Garden driveway" };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static float Cross(Vector3 a, Vector3 b) => a.x * b.z - a.z * b.x;

        private static void Subtract(List<Vector3> polygon, Vector3 a, Vector3 b, Vector3 c, List<List<Vector3>> result)
        {
            // Cheap rejection keeps untouched strips intact and avoids unnecessary fragments.
            var min = Vector3.Min(a, Vector3.Min(b, c)); var max = Vector3.Max(a, Vector3.Max(b, c));
            var pmin = polygon[0]; var pmax = pmin;
            foreach (var p in polygon) { pmin = Vector3.Min(pmin, p); pmax = Vector3.Max(pmax, p); }
            if (pmax.x <= min.x || pmin.x >= max.x || pmax.z <= min.z || pmin.z >= max.z)
            { result.Add(polygon); return; }
            var sign = Mathf.Sign(Cross(b-a, c-a));
            var clip = new[] { a, b, c };
            // Each outside slice survives. Only the inside remainder proceeds to the next
            // edge; the remainder after all three edges is the overlap and is discarded.
            for (var edge = 0; edge < 3 && polygon.Count >= 3; edge++)
            {
                var from = clip[edge]; var span = clip[(edge+1)%3] - from;
                var inside = new List<Vector3>(); var outside = new List<Vector3>();
                var previous = polygon[polygon.Count-1];
                var previousDistance = Cross(span, previous-from) * sign;
                foreach (var point in polygon)
                {
                    var distance = Cross(span, point-from) * sign;
                    if ((distance > 0) != (previousDistance > 0))
                    {
                        var intersection = Vector3.LerpUnclamped(previous, point, previousDistance / (previousDistance-distance));
                        inside.Add(intersection); outside.Add(intersection);
                    }
                    if (distance >= 0) inside.Add(point);
                    else outside.Add(point);
                    previous = point; previousDistance = distance;
                }
                if (outside.Count >= 3) result.Add(outside);
                polygon = inside;
            }
        }
    }
}

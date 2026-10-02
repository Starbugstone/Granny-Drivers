using System;
using System.Collections.Generic;
using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.Editor
{
    public static class CircuitLayout
    {
        // Orthogonal blocks with sampled round corners, including inward doglegs.
        public static TrackWaypoint[] Neighborhood(bool school)
        {
            var vertices = school
                ? new[] { new Vector2(-150,-100),new Vector2(150,-100),new Vector2(150,100),new Vector2(50,100),
                    new Vector2(50,30),new Vector2(-50,30),new Vector2(-50,100),new Vector2(-150,100) }
                : new[] { new Vector2(-130,-90),new Vector2(130,-90),new Vector2(130,90),
                    new Vector2(15,90),new Vector2(15,10),new Vector2(-130,10) };
            const float radius=20;
            var result=new List<TrackWaypoint>();
            var start=(vertices[0]+vertices[1])*.5f;
            result.Add(new TrackWaypoint(new Vector3(start.x,0,start.y),14,true));
            for(var step=1;step<=vertices.Length;step++)
            {
                var i=step%vertices.Length;
                var v=vertices[i];
                var incoming=(v-vertices[(i+vertices.Length-1)%vertices.Length]).normalized;
                var outgoing=(vertices[(i+1)%vertices.Length]-v).normalized;
                var center=v-incoming*radius+outgoing*radius;
                var radial=-outgoing*radius;
                var angle=Mathf.Atan2(radial.y,radial.x);
                var sign=Mathf.Sign(incoming.x*outgoing.y-incoming.y*outgoing.x);
                for(var j=0;j<=3;j++)
                {
                    var a=angle+sign*j*Mathf.PI/6;
                    var p=center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                    result.Add(new TrackWaypoint(new Vector3(p.x,0,p.y),12,false));
                }
                if(step<vertices.Length)
                {
                    var p=(v+vertices[(i+1)%vertices.Length])*.5f;
                    result.Add(new TrackWaypoint(new Vector3(p.x,0,p.y),12,true));
                }
            }
            return result.ToArray();
        }

        public static TrackWaypoint[] Square(float width = 240, float depth = 200, float radius = 18)
        {
            if (width < 100 || depth < 100 || radius < 14 || radius > Mathf.Min(width, depth) * .25f)
                throw new ArgumentException("Blocks need at least 100 m sides and corners of 14 m or more.");
            var points = new List<TrackWaypoint>();
            points.Add(new TrackWaypoint(new Vector3(0, 0, -depth / 2), 14, true));
            var centers = new[] { new Vector3(width / 2 - radius, 0, -depth / 2 + radius),
                new Vector3(width / 2 - radius, 0, depth / 2 - radius),
                new Vector3(-width / 2 + radius, 0, depth / 2 - radius),
                new Vector3(-width / 2 + radius, 0, -depth / 2 + radius) };
            for (var corner = 0; corner < 4; corner++)
            {
                for (var step = 0; step <= 3; step++)
                {
                    var angle = (-90 + corner * 90 + step * 30) * Mathf.Deg2Rad;
                    var position = centers[corner] + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                    points.Add(new TrackWaypoint(position, corner == 2 ? 8 : 12, step == 2));
                }
                if (corner < 3)
                {
                    var middle = corner == 0 ? new Vector3(width / 2, 0, 0)
                        : corner == 1 ? new Vector3(0, 0, depth / 2) : new Vector3(-width / 2, 0, 0);
                    points.Add(new TrackWaypoint(middle, corner == 1 ? 8 : 12, true));
                }
            }
            return points.ToArray();
        }

        public static TrackWaypoint[] Bake(CircuitRecipe recipe)
        {
            if (recipe == null) throw new ArgumentException("Select a circuit recipe.");
            ValidatePoints(recipe.controlPoints, false);
            if (recipe.laps < 1 || recipe.laps > 20) throw new ArgumentException("Choose 1–20 laps.");
            if (!Finite(recipe.lotSpacing) || recipe.lotSpacing < 12 || recipe.lotSpacing > 40
                || !Finite(recipe.houseSetback) || recipe.houseSetback < 5 || recipe.houseSetback > 25)
                throw new ArgumentException("Lot spacing must be 12–40 m; house setback must be 5–25 m.");
            var points = TrackLayoutBuilder.Resample(recipe.controlPoints, out _, 3, 6);
            ValidatePoints(points, true);
            ValidateShortcut(recipe);
            return points;
        }

        public static void ValidateShortcut(CircuitRecipe recipe)
        {
            if (!recipe.drivewayShortcut) return;
            var start = recipe.shortcutStartControl; var end = recipe.shortcutEndControl;
            if (start < 1 || end <= start + 1 || end >= recipe.controlPoints.Length
                || !Finite(recipe.shortcutWidth) || recipe.shortcutWidth < 3 || recipe.shortcutWidth > 6)
                throw new ArgumentException("Shortcut needs ordered start/end controls (not across the finish), and width 3–6 m.");
            for (var i = start + 1; i < end; i++)
                if (recipe.controlPoints[i].checkpoint)
                    throw new ArgumentException($"Shortcut skips checkpoint control {i}. Place shared checkpoints at or after the merge.");
            var a = recipe.controlPoints[start].position; var b = recipe.controlPoints[end].position;
            for (var i = 0; i < recipe.controlPoints.Length; i++)
            {
                if (i >= start - 1 && i <= end) continue;
                if (Crosses(a,b,recipe.controlPoints[i].position,recipe.controlPoints[(i+1)%recipe.controlPoints.Length].position))
                    throw new ArgumentException("Shortcut crosses an unrelated street. Keep branches inside one checkpoint sector.");
            }
        }

        public static bool ClearsShortcut(Vector3 center, float radius, CircuitRecipe recipe)
        {
            if (!recipe.drivewayShortcut) return true;
            var a=recipe.controlPoints[recipe.shortcutStartControl].position;
            var b=recipe.controlPoints[recipe.shortcutEndControl].position;
            return DistanceToSegment(center,a,b) > radius + recipe.shortcutWidth * .5f + 1;
        }

        public static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var span=b-a;
            return Vector3.Distance(point,a+span*Mathf.Clamp01(Vector3.Dot(point-a,span)/Mathf.Max(.001f,span.sqrMagnitude)));
        }

        public static void ValidatePoints(TrackWaypoint[] points, bool geometry)
        {
            if (points == null || points.Length < 4 || points.Length > 4096)
                throw new ArgumentException("Use 4–4096 distinct ordered route points.");
            var checkpointCount = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var point = points[i];
                if (!Finite(point.position.x) || !Finite(point.position.y) || !Finite(point.position.z)
                    || !Finite(point.width) || point.width < 6 || point.width > 18)
                    throw new ArgumentException($"Point {i}: coordinates must be finite and road width 6–18 m.");
                // First release is deliberately planar: lawns, extras and pavement share this plane.
                if (Mathf.Abs(point.position.y) > .001f)
                    throw new ArgumentException($"Point {i}: suburb circuits currently require Y = 0.");
                if (Vector3.Distance(point.position, points[(i + 1) % points.Length].position) < .05f)
                    throw new ArgumentException($"Point {i} coincides with its neighbour (including the closing seam).");
                if (point.checkpoint) checkpointCount++;
            }
            if (!points[0].checkpoint || checkpointCount < 4)
                throw new ArgumentException("Point zero must be the finish checkpoint; at least four checkpoints are required.");
            for (var i = 0; i < points.Length; i++)
            for (var j = i + 2; j < points.Length; j++)
            {
                if (i == 0 && j == points.Length - 1) continue;
                if (Crosses(points[i].position, points[(i + 1) % points.Length].position,
                    points[j].position, points[(j + 1) % points.Length].position))
                    throw new ArgumentException($"Route segments {i} and {j} cross. Use a single closed circuit.");
            }
            if (!geometry) return;
            GreyboxTrackGenerator.BuildRoadGeometry(points, out var vertices, out var triangles);
            // Check both expanded sidewalk outlines as well as the centerline. Two nearby
            // streets can have non-crossing centerlines while their drivable corridors overlap.
            var outline = (Vector3[])vertices.Clone();
            for (var i = 0; i < points.Length; i++)
            {
                var outward = (vertices[i*2+1]-vertices[i*2]).normalized * 3.5f;
                outline[i*2] -= outward; outline[i*2+1] += outward;
            }
            for (var i = 0; i < points.Length; i++)
            for (var j = i + 2; j < points.Length; j++)
            {
                if (i == 0 && j == points.Length - 1) continue;
                for (var a = 0; a <= 1; a++)
                for (var b = 0; b <= 1; b++)
                    if (Crosses(outline[i*2+a],outline[((i+1)%points.Length)*2+a],
                        outline[j*2+b],outline[((j+1)%points.Length)*2+b]))
                        throw new ArgumentException("Road/sidewalk corridors overlap. Separate the neighbouring streets.");
            }
            for (var i = 0; i < triangles.Length; i += 3)
                if (Vector3.Cross(vertices[triangles[i + 1]] - vertices[triangles[i]],
                    vertices[triangles[i + 2]] - vertices[triangles[i]]).y <= .0001f)
                    throw new ArgumentException("Road folds over itself. Open the corner or reduce its width.");
            for (var i = 0; i < points.Length; i++)
            {
                var previous = points[(i + points.Length - 1) % points.Length].position;
                var next = points[(i + 1) % points.Length].position;
                var turn = Vector3.Angle(points[i].position - previous, next - points[i].position) * Mathf.Deg2Rad;
                var radius = (Vector3.Distance(previous, points[i].position) + Vector3.Distance(next, points[i].position)) * .5f / Mathf.Max(turn, .0001f);
                if (radius < points[i].width * .5f + 4)
                    throw new ArgumentException($"Corner near sample {i} is too tight for its road and sidewalks.");
            }
        }

        public static bool ClearsRoad(Vector3 center, float radius, TrackWaypoint[] points, float margin = 4)
        {
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i].position;
                var b = points[(i + 1) % points.Length].position;
                var span = b - a;
                var t = Mathf.Clamp01(Vector3.Dot(center - a, span) / span.sqrMagnitude);
                var width = Mathf.Max(points[i].width, points[(i + 1) % points.Length].width);
                if (Vector3.Distance(center, a + t * span) < width * .5f + margin + radius) return false;
            }
            return true;
        }

        public static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static float Side(Vector3 a, Vector3 b, Vector3 p) => (b.x - a.x) * (p.z - a.z) - (b.z - a.z) * (p.x - a.x);
        private static bool Crosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d) =>
            Side(a, b, c) * Side(a, b, d) < 0 && Side(c, d, a) * Side(c, d, b) < 0;
    }
}

using System;
using System.Collections.Generic;
using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.Editor
{
    /// <summary>
    /// Expands a short list of authored control points into the dense, closed waypoint ring
    /// that <see cref="GreyboxTrackGenerator"/> consumes.
    /// </summary>
    /// <remarks>
    /// The generator draws straight road quads, kerbs, and barriers between consecutive
    /// waypoints, so the waypoint list <em>is</em> the geometry: a hairpin described by three
    /// waypoints comes out as a triangle. Rather than hand-authoring a hundred points, a
    /// layout is authored as a couple of dozen corner and straight markers and this class
    /// fills the gaps along a centripetal Catmull-Rom spline, spending samples only where
    /// curvature needs them. Straights stay cheap; corners stay round.
    ///
    /// Centripetal (alpha = 0.5) rather than the uniform parameterisation used by
    /// <see cref="TrackDefinition.EvaluateClosedSpline"/>, because layout control points are
    /// deliberately unevenly spaced — a 47 m straight next to a 17 m hairpin apex — and
    /// uniform Catmull-Rom overshoots into a cusp or a loop when spacing jumps like that.
    /// </remarks>
    public static class TrackLayoutBuilder
    {
        /// <summary>Longest gap allowed between two generated waypoints, in metres.</summary>
        public const float DefaultMaximumSpacing = 18f;

        /// <summary>Most a generated segment may turn before it is split again.</summary>
        public const float DefaultMaximumTurnDegrees = 15f;

        private const float Alpha = 0.5f;
        private const int MeasureSamples = 24;
        private const int MaximumSubdivisions = 64;

        /// <summary>
        /// Resamples <paramref name="control"/> into the waypoint ring to store on a
        /// <see cref="TrackDefinition"/>. Every control point survives verbatim — including
        /// its width and checkpoint flag — so authored indices stay meaningful; generated
        /// in-between points are never checkpoints.
        /// </summary>
        /// <param name="controlWaypointIndices">
        /// Where each control point ended up in the returned array. Callers that reference a
        /// control point by index (the shortcut mouths, for instance) must remap through this.
        /// </param>
        public static TrackWaypoint[] Resample(TrackWaypoint[] control,
            out int[] controlWaypointIndices,
            float maximumSpacing = DefaultMaximumSpacing,
            float maximumTurnDegrees = DefaultMaximumTurnDegrees)
        {
            if (control == null || control.Length < 3)
            {
                throw new ArgumentException("A closed track layout needs at least three control points.",
                    nameof(control));
            }

            if (maximumSpacing <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSpacing),
                    "Waypoint spacing must be positive.");
            }

            if (maximumTurnDegrees <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumTurnDegrees),
                    "Maximum turn per segment must be positive.");
            }

            var waypoints = new List<TrackWaypoint>(control.Length * 8);
            controlWaypointIndices = new int[control.Length];

            for (var i = 0; i < control.Length; i++)
            {
                controlWaypointIndices[i] = waypoints.Count;
                waypoints.Add(control[i]);

                var next = control[(i + 1) % control.Length];
                var steps = SubdivisionCount(control, i, maximumSpacing, maximumTurnDegrees);
                for (var step = 1; step < steps; step++)
                {
                    var t = (float)step / steps;
                    waypoints.Add(new TrackWaypoint(Evaluate(control, i, t),
                        Mathf.SmoothStep(control[i].width, next.width, t), false));
                }
            }

            return waypoints.ToArray();
        }

        /// <summary>
        /// Total length of the closed ring, measured along the chords the generator actually
        /// builds rather than along the ideal spline.
        /// </summary>
        public static float MeasureLapLength(TrackWaypoint[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 2) return 0f;
            var total = 0f;
            for (var i = 0; i < waypoints.Length; i++)
            {
                total += Vector3.Distance(waypoints[i].position,
                    waypoints[(i + 1) % waypoints.Length].position);
            }

            return total;
        }

        /// <summary>
        /// Radius of the tightest corner on the ring, estimated from the generated chords.
        /// </summary>
        /// <remarks>
        /// The number to compare against what the walker can hold: at speed v with a maximum
        /// yaw rate w rad/s, the tightest arc it can follow flat out is v / w metres. A lap
        /// whose tightest radius is above that has no corner worth braking for.
        /// </remarks>
        public static float MeasureTightestCornerRadius(TrackWaypoint[] waypoints)
        {
            if (waypoints == null || waypoints.Length < 3) return float.PositiveInfinity;
            var tightest = float.PositiveInfinity;
            for (var i = 0; i < waypoints.Length; i++)
            {
                var a = waypoints[i].position;
                var b = waypoints[(i + 1) % waypoints.Length].position;
                var c = waypoints[(i + 2) % waypoints.Length].position;
                var turn = Vector3.Angle(b - a, c - b) * Mathf.Deg2Rad;
                if (turn < 1e-3f) continue;
                var travelled = Vector3.Distance(a, b) + Vector3.Distance(b, c);
                tightest = Mathf.Min(tightest, travelled * 0.5f / turn);
            }

            return tightest;
        }

        private static int SubdivisionCount(TrackWaypoint[] control, int segment,
            float maximumSpacing, float maximumTurnDegrees)
        {
            MeasureSegment(control, segment, out var length, out var turnDegrees);
            var bySpacing = Mathf.CeilToInt(length / maximumSpacing);
            var byTurn = Mathf.CeilToInt(turnDegrees / maximumTurnDegrees);
            var steps = Mathf.Max(1, Mathf.Max(bySpacing, byTurn));

            // Curvature is rarely spread evenly along a segment: a control point placed near
            // a hairpin apex crams most of the turn into one end, and dividing the segment's
            // *total* turn by the budget then leaves a single facet holding most of it. Grow
            // the count until the sharpest facet the segment will really produce fits.
            while (steps < MaximumSubdivisions
                && SharpestFacetDegrees(control, segment, steps) > maximumTurnDegrees)
            {
                steps++;
            }

            return steps;
        }

        private static float SharpestFacetDegrees(TrackWaypoint[] control, int segment, int steps)
        {
            var sharpest = 0f;
            var previous = Evaluate(control, segment, 0f);
            var current = Evaluate(control, segment, 1f / steps);
            for (var i = 2; i <= steps; i++)
            {
                var next = Evaluate(control, segment, (float)i / steps);
                sharpest = Mathf.Max(sharpest, Vector3.Angle(current - previous, next - current));
                previous = current;
                current = next;
            }

            return sharpest;
        }

        /// <summary>
        /// Arc length and total absolute heading change across one control segment. Both are
        /// measured on the spline, not on the chord, so a segment that bulges out gets the
        /// subdivisions its real shape needs.
        /// </summary>
        private static void MeasureSegment(TrackWaypoint[] control, int segment, out float length,
            out float turnDegrees)
        {
            length = 0f;
            turnDegrees = 0f;
            var previousPoint = Evaluate(control, segment, 0f);
            var previousDirection = Vector3.zero;
            for (var i = 1; i <= MeasureSamples; i++)
            {
                var point = Evaluate(control, segment, (float)i / MeasureSamples);
                var step = point - previousPoint;
                length += step.magnitude;
                if (step.sqrMagnitude > 1e-8f)
                {
                    var direction = step / step.magnitude;
                    if (previousDirection != Vector3.zero)
                    {
                        turnDegrees += Vector3.Angle(previousDirection, direction);
                    }

                    previousDirection = direction;
                }

                previousPoint = point;
            }
        }

        private static Vector3 Evaluate(TrackWaypoint[] control, int segment, float t)
        {
            var count = control.Length;
            var p0 = control[(segment - 1 + count) % count].position;
            var p1 = control[segment % count].position;
            var p2 = control[(segment + 1) % count].position;
            var p3 = control[(segment + 2) % count].position;
            return CentripetalCatmullRom(p0, p1, p2, p3, Mathf.Clamp01(t));
        }

        private static Vector3 CentripetalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
            float u)
        {
            const float t0 = 0f;
            var t1 = t0 + Knot(p0, p1);
            var t2 = t1 + Knot(p1, p2);
            var t3 = t2 + Knot(p2, p3);
            var t = Mathf.Lerp(t1, t2, u);

            var a1 = Blend(p0, p1, t0, t1, t);
            var a2 = Blend(p1, p2, t1, t2, t);
            var a3 = Blend(p2, p3, t2, t3, t);
            var b1 = Blend(a1, a2, t0, t2, t);
            var b2 = Blend(a2, a3, t1, t3, t);
            return Blend(b1, b2, t1, t2, t);
        }

        // Floored above zero: coincident control points would otherwise divide by zero below.
        private static float Knot(Vector3 a, Vector3 b)
        {
            return Mathf.Max(Mathf.Pow(Vector3.Distance(a, b), Alpha), 1e-4f);
        }

        private static Vector3 Blend(Vector3 a, Vector3 b, float ta, float tb, float t)
        {
            var span = tb - ta;
            if (span <= 1e-6f) return b;
            return a * ((tb - t) / span) + b * ((t - ta) / span);
        }
    }
}

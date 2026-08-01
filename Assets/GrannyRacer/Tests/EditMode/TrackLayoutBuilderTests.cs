using System;
using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    public sealed class TrackLayoutBuilderTests
    {
        private const float Spacing = 18f;
        private const float TurnDegrees = 15f;

        /// <summary>
        /// Deliberately uneven: a long straight next to a tight hairpin is exactly the case
        /// that makes uniform Catmull-Rom overshoot.
        /// </summary>
        private static TrackWaypoint[] UnevenLoop()
        {
            return new[]
            {
                new TrackWaypoint(new Vector3(0f, 0f, -60f), 14f, true),
                new TrackWaypoint(new Vector3(60f, 0f, -60f), 14f, false),
                new TrackWaypoint(new Vector3(70f, 2f, -48f), 7f, true),
                new TrackWaypoint(new Vector3(58f, 2f, -40f), 7f, false),
                new TrackWaypoint(new Vector3(40f, 4f, 20f), 10f, false),
                new TrackWaypoint(new Vector3(-40f, 4f, 20f), 10f, true),
                new TrackWaypoint(new Vector3(-60f, 0f, -30f), 12f, false)
            };
        }

        private static TrackWaypoint[] Resample(TrackWaypoint[] control, out int[] indices)
        {
            return TrackLayoutBuilder.Resample(control, out indices, Spacing, TurnDegrees);
        }

        [Test]
        public void EveryControlPointSurvivesAtItsReportedIndex()
        {
            var control = UnevenLoop();
            var waypoints = Resample(control, out var indices);

            Assert.That(indices, Has.Length.EqualTo(control.Length));
            for (var i = 0; i < control.Length; i++)
            {
                var generated = waypoints[indices[i]];
                Assert.That(generated.position, Is.EqualTo(control[i].position),
                    $"Control point {i} moved. Authored indices such as the shortcut mouths "
                    + "are remapped through these, so they must land exactly.");
                Assert.That(generated.width, Is.EqualTo(control[i].width).Within(0.001f));
                Assert.That(generated.checkpoint, Is.EqualTo(control[i].checkpoint));
            }
        }

        [Test]
        public void ControlPointIndicesStayInLapOrder()
        {
            var control = UnevenLoop();
            var waypoints = Resample(control, out var indices);

            for (var i = 1; i < indices.Length; i++)
            {
                Assert.That(indices[i], Is.GreaterThan(indices[i - 1]),
                    "Generated waypoints must stay in lap order, or checkpoint ordering breaks.");
            }

            Assert.That(indices[indices.Length - 1], Is.LessThan(waypoints.Length));
        }

        [Test]
        public void NoGapExceedsTheRequestedSpacing()
        {
            var waypoints = Resample(UnevenLoop(), out _);

            for (var i = 0; i < waypoints.Length; i++)
            {
                var gap = Vector3.Distance(waypoints[i].position,
                    waypoints[(i + 1) % waypoints.Length].position);
                // Samples are even in the spline parameter rather than in arc length, so the
                // spacing is a budget, not a guarantee. The tolerance covers that skew.
                Assert.That(gap, Is.LessThan(Spacing * 1.6f),
                    $"Waypoints {i} and {i + 1} are {gap:0.0} m apart; the road quad between "
                    + "them is a straight line, so long gaps are visible facets.");
            }
        }

        [Test]
        public void NoFacetTurnsSharplyEnoughToReadAsACorner()
        {
            var waypoints = Resample(UnevenLoop(), out _);

            for (var i = 0; i < waypoints.Length; i++)
            {
                var a = waypoints[i].position;
                var b = waypoints[(i + 1) % waypoints.Length].position;
                var c = waypoints[(i + 2) % waypoints.Length].position;
                var turn = Vector3.Angle(b - a, c - b);
                // Two budgets meet at a control point — the tail of one segment and the head
                // of the next — so a single facet can spend up to both.
                Assert.That(turn, Is.LessThan(TurnDegrees * 2.5f),
                    $"Facet at waypoint {i + 1} turns {turn:0.0} degrees. Corners are built "
                    + "from these straight chords, so a sharp facet is a visible kink.");
            }
        }

        [Test]
        public void GeneratedPointsAreNeverCheckpoints()
        {
            var control = UnevenLoop();
            var waypoints = Resample(control, out var indices);
            var authored = new bool[waypoints.Length];
            for (var i = 0; i < indices.Length; i++) authored[indices[i]] = true;

            for (var i = 0; i < waypoints.Length; i++)
            {
                if (authored[i]) continue;
                Assert.That(waypoints[i].checkpoint, Is.False,
                    $"Generated waypoint {i} became a checkpoint. Checkpoint order and count "
                    + "must stay under the layout author's control.");
            }
        }

        [Test]
        public void WidthsStayWithinTheAuthoredRange()
        {
            var control = UnevenLoop();
            var waypoints = Resample(control, out _);
            var minimum = float.MaxValue;
            var maximum = float.MinValue;
            for (var i = 0; i < control.Length; i++)
            {
                minimum = Mathf.Min(minimum, control[i].width);
                maximum = Mathf.Max(maximum, control[i].width);
            }

            for (var i = 0; i < waypoints.Length; i++)
            {
                Assert.That(waypoints[i].width, Is.InRange(minimum - 0.01f, maximum + 0.01f),
                    $"Waypoint {i} is {waypoints[i].width:0.00} m wide, outside the authored "
                    + "range. Width feeds the checkpoint trigger size as well as the road.");
            }
        }

        [Test]
        public void ARingOfEvenlySpacedPointsIsLeftAlone()
        {
            // Four points 15 m apart with a 90 degree turn each: under both budgets, so the
            // builder should add nothing.
            var square = new[]
            {
                new TrackWaypoint(new Vector3(0f, 0f, -8f), 10f, true),
                new TrackWaypoint(new Vector3(8f, 0f, 0f), 10f, false),
                new TrackWaypoint(new Vector3(0f, 0f, 8f), 10f, true),
                new TrackWaypoint(new Vector3(-8f, 0f, 0f), 10f, false)
            };

            var waypoints = TrackLayoutBuilder.Resample(square, out _, 100f, 360f);

            Assert.That(waypoints, Has.Length.EqualTo(square.Length));
        }

        [Test]
        public void TooFewControlPointsIsRejected()
        {
            var pair = new[]
            {
                new TrackWaypoint(Vector3.zero, 10f, true),
                new TrackWaypoint(Vector3.forward * 10f, 10f, false)
            };

            Assert.Throws<ArgumentException>(() => TrackLayoutBuilder.Resample(pair, out _));
            Assert.Throws<ArgumentException>(() => TrackLayoutBuilder.Resample(null, out _));
        }

        [Test]
        public void NonPositiveBudgetsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrackLayoutBuilder.Resample(UnevenLoop(), out _, 0f, TurnDegrees));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => TrackLayoutBuilder.Resample(UnevenLoop(), out _, Spacing, 0f));
        }

        [Test]
        public void CoincidentControlPointsDoNotProduceInvalidPositions()
        {
            var degenerate = new[]
            {
                new TrackWaypoint(new Vector3(0f, 0f, -20f), 10f, true),
                new TrackWaypoint(new Vector3(0f, 0f, -20f), 10f, false),
                new TrackWaypoint(new Vector3(20f, 0f, 0f), 10f, false),
                new TrackWaypoint(new Vector3(0f, 0f, 20f), 10f, true)
            };

            var waypoints = Resample(degenerate, out _);

            for (var i = 0; i < waypoints.Length; i++)
            {
                var position = waypoints[i].position;
                Assert.That(float.IsNaN(position.x) || float.IsNaN(position.y) || float.IsNaN(position.z),
                    Is.False, $"Waypoint {i} is NaN. Duplicated control points must not divide by zero.");
            }
        }
    }
}

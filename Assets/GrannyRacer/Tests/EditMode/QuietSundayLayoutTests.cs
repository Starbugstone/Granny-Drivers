using GrannyRacer.Editor;
using GrannyRacer.Racing;
using NUnit.Framework;
using UnityEngine;

namespace GrannyRacer.Tests.EditMode
{
    /// <summary>
    /// Guards the route features playbook §17.3 asks the POC circuit to provide. These are
    /// shape checks, not quality checks — whether the circuit is fun to drive is a human
    /// playtest question.
    /// </summary>
    public sealed class QuietSundayLayoutTests
    {
        private static TrackDefinition BuildTrack()
        {
            var track = ScriptableObject.CreateInstance<TrackDefinition>();
            QuietSundayLayout.Apply(track);
            return track;
        }

        [Test]
        public void LapIsLongEnoughToSeparateItsCorners()
        {
            var track = BuildTrack();
            var length = TrackLayoutBuilder.MeasureLapLength(track.Waypoints);

            // The first prototype loop was ~190 m, which at the 15 m/s cap put every corner
            // inside the previous one's recovery, so a handling change could not be
            // attributed to anything in particular.
            Assert.That(length, Is.GreaterThan(700f),
                $"Lap is only {length:0} m; the layout is meant to give each feature room.");
        }

        [Test]
        public void CheckpointsAreOrderedAndEvenlySpread()
        {
            var track = BuildTrack();
            var points = track.Waypoints;
            var checkpointCount = 0;
            var longestGap = 0f;
            var gap = 0f;

            for (var i = 0; i < points.Length; i++)
            {
                gap += Vector3.Distance(points[i].position, points[(i + 1) % points.Length].position);
                if (!points[(i + 1) % points.Length].checkpoint) continue;
                checkpointCount++;
                longestGap = Mathf.Max(longestGap, gap);
                gap = 0f;
            }

            Assert.That(checkpointCount, Is.GreaterThanOrEqualTo(6),
                "Reset-to-checkpoint is only as forgiving as the checkpoints are dense.");
            Assert.That(longestGap, Is.LessThan(180f),
                $"The longest run between checkpoints is {longestGap:0} m, which is a long way "
                + "to redrive after a reset.");
        }

        [Test]
        public void RouteOffersAWideStraightABottleneckAndRealElevation()
        {
            var control = QuietSundayLayout.ControlPoints();
            var widest = 0f;
            var narrowest = float.MaxValue;
            var lowest = float.MaxValue;
            var highest = float.MinValue;

            for (var i = 0; i < control.Length; i++)
            {
                widest = Mathf.Max(widest, control[i].width);
                narrowest = Mathf.Min(narrowest, control[i].width);
                lowest = Mathf.Min(lowest, control[i].position.y);
                highest = Mathf.Max(highest, control[i].position.y);
            }

            Assert.That(widest, Is.GreaterThanOrEqualTo(14f), "No wide overtaking section.");
            Assert.That(narrowest, Is.LessThanOrEqualTo(7f), "No bottleneck.");
            Assert.That(highest - lowest, Is.GreaterThanOrEqualTo(8f),
                "The downhill booster section needs a hill to run down.");
        }

        [Test]
        public void RouteContainsACornerTooTightToTakeAtTopSpeed()
        {
            var track = BuildTrack();
            var tightestRadius = TrackLayoutBuilder.MeasureTightestCornerRadius(track.Waypoints);

            // WalkerHandling_POC steers 125 deg/s scaled to 0.42 at the 15 m/s cap, which is
            // 0.92 rad/s, so the tightest arc holdable flat out is about 16 m. The hairpin
            // must be under that or it is not a braking decision.
            Assert.That(tightestRadius, Is.LessThan(16f),
                $"Tightest corner radius is {tightestRadius:0.0} m, which the walker can hold "
                + "at top speed. Nothing on the lap then rewards heat management.");
        }

        [Test]
        public void ShortcutIsShorterThanTheRoadItBypassesAndSkipsNoCheckpoint()
        {
            var track = BuildTrack();
            var points = track.Waypoints;
            var start = track.ShortcutStartWaypoint;
            var end = track.ShortcutEndWaypoint;

            Assert.That(start, Is.InRange(0, points.Length - 1));
            Assert.That(end, Is.InRange(0, points.Length - 1));
            Assert.That(end, Is.GreaterThan(start));

            var road = 0f;
            for (var i = start; i < end; i++)
            {
                road += Vector3.Distance(points[i].position, points[i + 1].position);
            }

            for (var i = start + 1; i < end; i++)
            {
                Assert.That(points[i].checkpoint, Is.False,
                    $"Waypoint {i} is a checkpoint inside the shortcut, so taking the shortcut "
                    + "would stall the racer's lap progress.");
            }

            var chord = Vector3.Distance(points[start].position, points[end].position);
            Assert.That(chord, Is.LessThan(road),
                $"The shortcut ({chord:0} m) is no shorter than the road it bypasses ({road:0} m).");
            Assert.That(chord, Is.GreaterThan(road * 0.5f),
                $"The shortcut ({chord:0} m) removes more than half of a {road:0} m section, "
                + "which makes it the only line worth driving rather than a risk worth taking.");
        }

        /// <summary>
        /// The shortcut indices are authored against <see cref="QuietSundayLayout.ControlPoints"/>
        /// but stored against the generated ring, so an off-by-one here would silently move
        /// the driveway to the wrong corner.
        /// </summary>
        [Test]
        public void ShortcutMouthsLandOnTheAuthoredControlPoints()
        {
            var control = QuietSundayLayout.ControlPoints();
            var track = BuildTrack();

            Assert.That(track.Waypoints[track.ShortcutStartWaypoint].position,
                Is.EqualTo(control[QuietSundayLayout.ShortcutEntryControl].position));
            Assert.That(track.Waypoints[track.ShortcutEndWaypoint].position,
                Is.EqualTo(control[QuietSundayLayout.ShortcutExitControl].position));
        }
    }
}

using GrannyRacer.Racing;
using UnityEngine;

namespace GrannyRacer.Editor
{
    /// <summary>
    /// The authored control points for the POC greybox circuit, "Quiet Sunday".
    /// </summary>
    /// <remarks>
    /// Roughly 800 m over a 232 x 219 m footprint — about a minute a lap at the POC's 15 m/s
    /// cap, four times the first prototype loop. The extra size exists to make the handling
    /// testable: the first loop was small enough that every corner ran into the next one, so
    /// a tuning change could not be attributed to anything in particular.
    ///
    /// Covers the route features playbook §17.3 asks for, in lap order from the start line:
    ///
    /// <list type="bullet">
    /// <item>wide overtaking straight — 16 m, 136 m of it before the first braking point</item>
    /// <item>fast climbing right-hander — flat out, rising to a 12.5 m crest</item>
    /// <item>downhill booster section — wide and straight, 12 m of drop over ~95 m</item>
    /// <item>sharp corner where heat management matters — a 7 m wide, 10 m radius hairpin,
    ///       tighter than the walker can hold at top speed, so it has to be braked for</item>
    /// <item>fast crescent — a sweeper that bulges north, and the alternative route to…</item>
    /// <item>risky driveway shortcut — 5 m wide and straight across the crescent's base,
    ///       saving about 20 m for anyone who can hit the mouth at speed</item>
    /// <item>attack bottleneck — 6 m wide and ~75 m long, no room to pass</item>
    /// <item>descending right-left — a committed change of direction while losing height</item>
    /// <item>tight final corner — 12 m radius back onto the start straight</item>
    /// </list>
    ///
    /// These are control points, not the finished track. <see cref="TrackLayoutBuilder"/>
    /// fills the corners in; Docs/POC_TRACK_LAYOUT.md covers how to regenerate.
    /// </remarks>
    public static class QuietSundayLayout
    {
        public const int Laps = 3;
        public const float ShortcutWidth = 5f;

        /// <summary>
        /// Entry to the crescent, where the driveway leaves the road. Indices into
        /// <see cref="ControlPoints"/>, remapped to real waypoint indices by <see cref="Apply"/>.
        /// </summary>
        public const int ShortcutEntryControl = 18;

        /// <summary>Where the driveway rejoins, at the mouth of the bottleneck.</summary>
        public const int ShortcutExitControl = 24;

        /// <remarks>
        /// Corners are described by several points spaced evenly along the arc, not by a
        /// single apex marker. A lone apex between two distant markers makes the spline pinch:
        /// the first pass authored the hairpin as three points and produced a 5.3 m radius
        /// where 10 m was intended, tight enough to fold the inner barrier into itself.
        /// </remarks>
        public static TrackWaypoint[] ControlPoints()
        {
            return new[]
            {
                // Start / finish, heading east down the overtaking straight.
                new TrackWaypoint(new Vector3(-70f, 0.05f, -115f), 16f, true),
                new TrackWaypoint(new Vector3(-23f, 0.05f, -115f), 16f, false),
                new TrackWaypoint(new Vector3(23f, 0.05f, -115f), 16f, true),

                // Climbing right-hander up to the crest. Meant to be flat out.
                new TrackWaypoint(new Vector3(66f, 1.2f, -109f), 13f, false),
                new TrackWaypoint(new Vector3(95f, 4f, -85f), 12f, false),
                new TrackWaypoint(new Vector3(109f, 8f, -50f), 11f, true),
                new TrackWaypoint(new Vector3(108f, 12.5f, -13f), 12f, false),

                // Downhill booster section: wide, straight, and the fastest part of the lap.
                new TrackWaypoint(new Vector3(95f, 9.5f, 19f), 14f, false),
                new TrackWaypoint(new Vector3(81f, 4.5f, 47f), 14f, true),
                new TrackWaypoint(new Vector3(70f, 0.6f, 75f), 12f, false),

                // Hairpin: a 10 m radius arc, narrowing to 7 m. Tighter than the walker can
                // hold at the 15 m/s cap, so the approach is a braking decision taken on
                // whatever slipper heat the downhill left behind.
                new TrackWaypoint(new Vector3(68f, 0.05f, 96f), 9f, false),
                new TrackWaypoint(new Vector3(66.3f, 0.05f, 100.1f), 7.5f, false),
                new TrackWaypoint(new Vector3(62.9f, 0.05f, 103.1f), 7f, false),
                new TrackWaypoint(new Vector3(58.6f, 0.05f, 104.3f), 7f, true),
                new TrackWaypoint(new Vector3(54.1f, 0.05f, 103.4f), 7f, false),
                new TrackWaypoint(new Vector3(50.6f, 0.05f, 100.8f), 7.5f, false),

                // Chute out of the hairpin, then a fast sweeper onto the westbound run.
                new TrackWaypoint(new Vector3(39f, 0.3f, 88f), 9f, false),
                new TrackWaypoint(new Vector3(18f, 1f, 79f), 10f, false),

                // The crescent: a fast sweeper that bulges 27 m north. The driveway shortcut
                // cuts straight across its base, which is where the 20 m saving comes from.
                new TrackWaypoint(new Vector3(-12f, 2f, 78f), 11f, true), // shortcut mouth
                new TrackWaypoint(new Vector3(-25f, 2.6f, 88f), 10f, false),
                new TrackWaypoint(new Vector3(-40f, 3.3f, 97f), 9f, false),
                new TrackWaypoint(new Vector3(-56f, 4f, 99f), 9f, false),
                new TrackWaypoint(new Vector3(-70f, 4.8f, 92f), 8f, false),
                new TrackWaypoint(new Vector3(-83f, 5.5f, 82f), 8f, false),
                new TrackWaypoint(new Vector3(-93f, 6.2f, 68f), 7f, false), // shortcut rejoin

                // Bottleneck: 6 m wide, ~75 m long, and no room to pass in any of it.
                new TrackWaypoint(new Vector3(-110f, 7f, 58f), 6f, false),
                new TrackWaypoint(new Vector3(-122f, 7f, 34f), 6f, true),
                new TrackWaypoint(new Vector3(-123f, 5.5f, 4f), 6f, false),

                // Long descent south, opening out, with a quick right-left to commit to.
                new TrackWaypoint(new Vector3(-116f, 4.2f, -20f), 9f, false),
                new TrackWaypoint(new Vector3(-104f, 3f, -38f), 10f, false),
                new TrackWaypoint(new Vector3(-108f, 1.8f, -62f), 11f, false),
                new TrackWaypoint(new Vector3(-114f, 1f, -85f), 13f, true),

                // Final corner: a 15 m radius left back onto the start straight. Radius and
                // width are not independent — the inner barrier sits half a width plus 0.8 m
                // inside the centreline, so a 15 m road on a 10 m corner rings it with a 2 m
                // circle of overlapping boxes. This corner stays narrower than the straight.
                new TrackWaypoint(new Vector3(-112.9f, 0.2f, -101.6f), 13f, false),
                new TrackWaypoint(new Vector3(-110.4f, 0.1f, -108.4f), 12f, false),
                new TrackWaypoint(new Vector3(-105f, 0.05f, -113.2f), 12f, false),
                new TrackWaypoint(new Vector3(-98f, 0.05f, -115f), 14f, false)
            };
        }

        /// <summary>
        /// Overwrites <paramref name="track"/> with the generated ring. Callers are
        /// responsible for marking the asset dirty and saving it.
        /// </summary>
        public static TrackWaypoint[] Apply(TrackDefinition track)
        {
            var waypoints = TrackLayoutBuilder.Resample(ControlPoints(), out var controlIndices);
            track.SetPrototypeData(waypoints, Laps, controlIndices[ShortcutEntryControl],
                controlIndices[ShortcutExitControl], ShortcutWidth);
            return waypoints;
        }
    }
}

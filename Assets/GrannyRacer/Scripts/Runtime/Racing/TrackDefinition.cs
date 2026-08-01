using System;
using UnityEngine;

namespace GrannyRacer.Racing
{
    [Serializable]
    public struct TrackWaypoint
    {
        public Vector3 position;
        [Min(3f)] public float width;
        public bool checkpoint;

        public TrackWaypoint(Vector3 position, float width, bool checkpoint)
        {
            this.position = position;
            this.width = width;
            this.checkpoint = checkpoint;
        }
    }

    [CreateAssetMenu(menuName = "Granny Racer/Track Definition", fileName = "Track_QuietSunday_POC")]
    public sealed class TrackDefinition : ScriptableObject
    {
        [SerializeField] private TrackWaypoint[] waypoints = Array.Empty<TrackWaypoint>();
        [SerializeField, Min(1)] private int laps = 3;
        [SerializeField] private int shortcutStartWaypoint = 2;
        [SerializeField] private int shortcutEndWaypoint = 4;
        [SerializeField, Min(3f)] private float shortcutWidth = 4.5f;

        public TrackWaypoint[] Waypoints => waypoints;
        public int Laps => laps;
        public int ShortcutStartWaypoint => shortcutStartWaypoint;
        public int ShortcutEndWaypoint => shortcutEndWaypoint;
        public float ShortcutWidth => shortcutWidth;

        public Vector3 EvaluateClosedSpline(float normalisedDistance)
        {
            if (waypoints == null || waypoints.Length == 0) return Vector3.zero;
            if (waypoints.Length == 1) return waypoints[0].position;

            var wrapped = normalisedDistance - Mathf.Floor(normalisedDistance);
            var scaled = wrapped * waypoints.Length;
            var index = Mathf.FloorToInt(scaled);
            var t = scaled - index;
            var p0 = waypoints[(index - 1 + waypoints.Length) % waypoints.Length].position;
            var p1 = waypoints[index % waypoints.Length].position;
            var p2 = waypoints[(index + 1) % waypoints.Length].position;
            var p3 = waypoints[(index + 2) % waypoints.Length].position;
            return CatmullRom(p0, p1, p2, p3, t);
        }

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var t2 = t * t;
            var t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

#if UNITY_EDITOR
        public void SetPrototypeData(TrackWaypoint[] prototypeWaypoints, int lapCount,
            int shortcutStart, int shortcutEnd, float prototypeShortcutWidth)
        {
            waypoints = prototypeWaypoints;
            laps = Mathf.Max(1, lapCount);
            shortcutStartWaypoint = shortcutStart;
            shortcutEndWaypoint = shortcutEnd;
            shortcutWidth = prototypeShortcutWidth;
        }
#endif
    }
}

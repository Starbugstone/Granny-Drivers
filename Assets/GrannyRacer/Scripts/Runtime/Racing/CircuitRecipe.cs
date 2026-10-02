using System;
using UnityEngine;

namespace GrannyRacer.Racing
{
    public enum CircuitAssetRole { House, Tree, Fence, StreetFurniture, Landmark }

    [Serializable]
    public sealed class CircuitAssetEntry
    {
        public string label;
        public GameObject prefab;
        public CircuitAssetRole role;
        [Min(.1f)] public float scale = 1f;
        [Tooltip("Rotation around the upright placement wrapper; preserves FBX axis conversion.")]
        public float yaw;
    }

    [Serializable]
    public sealed class CircuitPlacement
    {
        public GameObject prefab;
        public Vector3 position;
        public float yaw;
        [Min(.1f)] public float scale = 1;
    }

    [CreateAssetMenu(menuName = "Granny Racer/Circuit Recipe", fileName = "Circuit_Suburb")]
    public sealed class CircuitRecipe : ScriptableObject
    {
        public string circuitName = "SUNDAY BLOCK PARTY";
        [Min(1)] public int laps = 3;
        [Min(1)] public int seed = 17;
        [Range(12, 40)] public float lotSpacing = 20;
        [Range(5, 25)] public float houseSetback = 14;
        [Tooltip("Adds forward-only sector gates and offset boost panels on long straights.")]
        public bool arcadeFeatures = true;
        [Tooltip("Adds porch paths, parked cars and distant residential rows using the neighborhood expansion kit.")]
        public bool neighborhoodDetails;
        public bool drivewayShortcut;
        [Min(0)] public int shortcutStartControl = 5;
        [Min(0)] public int shortcutEndControl = 10;
        [Range(3, 6)] public float shortcutWidth = 4.5f;
        [Tooltip("The last point connects to the first. Point zero must be a checkpoint.")]
        public TrackWaypoint[] controlPoints = Array.Empty<TrackWaypoint>();
        public CircuitAssetEntry[] palette = Array.Empty<CircuitAssetEntry>();
        [Tooltip("Authored placements survive rebuilds. Keep their complete footprints outside the road and sidewalk.")]
        public CircuitPlacement[] extraAssets = Array.Empty<CircuitPlacement>();
    }
}

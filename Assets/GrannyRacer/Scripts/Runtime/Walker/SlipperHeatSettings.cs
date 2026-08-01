using UnityEngine;

namespace GrannyRacer.Walker
{
    [CreateAssetMenu(menuName = "Granny Racer/Slipper Heat", fileName = "SlipperHeat_POC")]
    public sealed class SlipperHeatSettings : ScriptableObject
    {
        [Min(0f)] public float heatPerSecond = 0.31f;
        [Min(0f)] public float collisionHeat = 0.08f;
        [Min(0f)] public float coolingDelay = 0.55f;
        [Min(0f)] public float coolingPerSecond = 0.2f;
        [Range(0f, 1f)] public float warningThreshold = 0.6f;
        [Range(0f, 1f)] public float criticalThreshold = 0.8f;
        [Min(0.1f)] public float replacementDuration = 3.4f;
        [Min(0f)] public float replacementTapReduction = 0.18f;
        [Range(0f, 1f)] public float postReplacementHeat = 0.2f;
        [Range(0.1f, 1f)] public float burnoutSpeedScale = 0.38f;
    }
}

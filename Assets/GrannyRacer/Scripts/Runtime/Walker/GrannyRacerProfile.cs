using UnityEngine;

namespace GrannyRacer.Walker
{
    /// <summary>
    /// Selectable character data layered over the shared walker handling. Keeping character
    /// identity separate from the controller lets every granny use the same racer prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Granny Racer/Granny Racer Profile", fileName = "GrannyProfile_Balanced")]
    public sealed class GrannyRacerProfile : ScriptableObject
    {
        [SerializeField] private string displayName = "Balanced Granny";

        [Header("Driving stat multipliers")]
        [Tooltip("Forward, reverse, and boost acceleration relative to the base walker.")]
        [Range(0.5f, 1.5f)] [SerializeField] private float acceleration = 1f;

        [Tooltip("Lateral grip relative to the base walker. Lower values slide more.")]
        [Range(0.5f, 1.5f)] [SerializeField] private float adherence = 1f;

        [Tooltip("Forward, reverse, and boost speed limits relative to the base walker.")]
        [Range(0.5f, 1.5f)] [SerializeField] private float maximumSpeed = 1f;

        public string DisplayName => displayName;
        public float Acceleration => acceleration;
        public float Adherence => adherence;
        public float MaximumSpeed => maximumSpeed;

        public GrannyStatMultipliers CreateMultipliers()
        {
            return new GrannyStatMultipliers(acceleration, adherence, maximumSpeed);
        }
    }
}

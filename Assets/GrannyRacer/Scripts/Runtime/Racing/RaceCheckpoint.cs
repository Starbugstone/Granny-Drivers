using GrannyRacer.Walker;
using UnityEngine;

namespace GrannyRacer.Racing
{
    [RequireComponent(typeof(Collider))]
    public sealed class RaceCheckpoint : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private RaceController race;

        public void Configure(int checkpointIndex, RaceController controller)
        {
            index = checkpointIndex;
            race = controller;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (race != null && other.TryGetComponent<ArcadeWalkerController>(out var racer))
            {
                race.TryPassCheckpoint(index, transform, racer);
            }
        }
    }
}

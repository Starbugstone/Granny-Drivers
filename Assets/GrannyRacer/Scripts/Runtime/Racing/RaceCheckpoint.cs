using GrannyRacer.Walker;
using UnityEngine;

namespace GrannyRacer.Racing
{
    [RequireComponent(typeof(Collider))]
    public sealed class RaceCheckpoint : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private RaceController race;
        [SerializeField] private bool requireForwardCrossing;
        [SerializeField] private Transform resetPose;
        private ArcadeWalkerController[] tracked;
        private float[] previousDistance;
        public int Index => index;

        public void Configure(int checkpointIndex, RaceController controller)
        {
            index = checkpointIndex;
            race = controller;
        }

        public void ConfigurePassage(Transform safeReset)
        {
            requireForwardCrossing = true;
            resetPose = safeReset;
        }

        private void Awake()
        {
            var count = race == null ? 1 : Mathf.Max(1, race.RacerCount);
            tracked = new ArcadeWalkerController[count];
            previousDistance = new float[count];
        }

        private void OnTriggerEnter(Collider other)
        {
            if (race != null && other.TryGetComponent<ArcadeWalkerController>(out var racer))
            {
                if (!requireForwardCrossing) race.TryPassCheckpoint(index, transform, racer);
                else CheckCrossing(other, racer, true);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (requireForwardCrossing && race != null && other.TryGetComponent<ArcadeWalkerController>(out var racer))
                CheckCrossing(other, racer, false);
        }

        private void CheckCrossing(Collider collider, ArcadeWalkerController racer, bool entering)
        {
            var body = collider.attachedRigidbody;
            if (body == null) return;
            var distance = Vector3.Dot(body.position - transform.position, transform.forward);
            var speed = Vector3.Dot(body.linearVelocity, transform.forward);
            for (var i = 0; i < tracked.Length; i++)
            {
                if (tracked[i] != null && tracked[i] != racer) continue;
                if (entering || tracked[i] == null)
                {
                    tracked[i] = racer;
                    previousDistance[i] = distance - speed * Time.fixedDeltaTime;
                }
                if (CheckpointCrossing.Forward(previousDistance[i], distance, speed))
                    race.TryPassCheckpoint(index, transform, racer, resetPose);
                previousDistance[i] = distance;
                return;
            }
        }
    }
}

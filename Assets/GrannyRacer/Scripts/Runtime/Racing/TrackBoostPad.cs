using GrannyRacer.Walker;
using UnityEngine;

namespace GrannyRacer.Racing
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrackBoostPad : MonoBehaviour
    {
        [SerializeField, Range(1, 10)] private float speedGain = 5;
        [SerializeField, Range(.1f, 3)] private float duration = 1;

        private void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || Vector3.Dot(body.linearVelocity, transform.forward) < 1) return;
            if (other.TryGetComponent<ArcadeWalkerController>(out var racer)) racer.TryApplyTrackBoost(speedGain, duration);
        }
    }
}

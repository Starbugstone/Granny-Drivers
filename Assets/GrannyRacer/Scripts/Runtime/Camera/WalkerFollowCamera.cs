using UnityEngine;

namespace GrannyRacer.Camera
{
    public sealed class WalkerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.4f, -4.2f);
        [SerializeField, Min(0f)] private float lookHeight = 0.9f;
        [SerializeField, Min(0f)] private float positionSharpness = 7f;
        [SerializeField, Min(0f)] private float rotationSharpness = 9f;

        public Vector3 Offset => offset;

        public void Configure(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            var rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            var desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);
            var desiredRotation = Quaternion.LookRotation(
                target.position + Vector3.up * lookHeight - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }
    }
}

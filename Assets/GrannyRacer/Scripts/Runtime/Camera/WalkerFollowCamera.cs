using UnityEngine;

namespace GrannyRacer.Camera
{
    public sealed class WalkerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4.5f, -7.5f);
        [SerializeField, Min(0f)] private float positionSharpness = 7f;
        [SerializeField, Min(0f)] private float rotationSharpness = 9f;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            var rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            var desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);
            var desiredRotation = Quaternion.LookRotation(target.position + Vector3.up * 1.2f - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
        }
    }
}

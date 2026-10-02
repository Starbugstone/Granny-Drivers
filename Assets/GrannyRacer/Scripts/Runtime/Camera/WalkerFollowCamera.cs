using UnityEngine;
using GrannyRacer.Walker;

namespace GrannyRacer.Camera
{
    public sealed class WalkerFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2.4f, -4.2f);
        [SerializeField, Min(0f)] private float lookHeight = 0.9f;
        [SerializeField, Min(0f)] private float positionSharpness = 7f;
        [SerializeField, Min(0f)] private float rotationSharpness = 9f;
        [Header("Arcade speed response")]
        [SerializeField] private float cruisingFieldOfView = 74f;
        [SerializeField] private float boostFieldOfView = 82f;
        [SerializeField] private float speedLookAhead = 3f;
        private ArcadeWalkerController walker;
        private UnityEngine.Camera view;
        private float restFieldOfView;

        public Vector3 Offset => offset;

        public void Configure(Transform followTarget, Vector3 followOffset)
        {
            target = followTarget;
            offset = followOffset;
        }

        private void Start()
        {
            if (target != null) walker = target.GetComponent<ArcadeWalkerController>();
            view = GetComponent<UnityEngine.Camera>();
            if (view != null) restFieldOfView = view.fieldOfView;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            var positionT = 1f - Mathf.Exp(-positionSharpness * Time.deltaTime);
            var rotationT = 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime);
            var speed = walker == null ? 0f : Mathf.Clamp01(walker.Speed / Mathf.Max(1, walker.Stats.MaximumSpeed));
            var boost = walker != null && (walker.IsBoosting || walker.DriftBoostActive
                || walker.IsTrackBoostActive || walker.IsStartBoostActive);
            var followOffset = offset + new Vector3(0, -.35f * speed, boost ? -.5f : 0);
            // Predict the camera's normal follow lag so high speed doesn't leave Granny tiny
            // in the distance. Retain smoothing for hops and collisions.
            var desiredPosition = target.TransformPoint(followOffset)
                + target.forward * (walker == null ? 0 : walker.Speed / Mathf.Max(1, positionSharpness));
            transform.position = Vector3.Lerp(transform.position, desiredPosition, positionT);
            var desiredRotation = Quaternion.LookRotation(
                target.position + Vector3.up * lookHeight + target.forward * (speedLookAhead * speed)
                    - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationT);
            if (view != null)
                view.fieldOfView = Mathf.Lerp(view.fieldOfView, boost ? boostFieldOfView
                    : Mathf.Lerp(restFieldOfView, cruisingFieldOfView, speed), rotationT);
        }
    }
}

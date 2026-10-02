#if UNITY_EDITOR || DEVELOPMENT_BUILD
using GrannyRacer.Walker;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace GrannyRacer.UI
{
    /// <summary>Development review input only. Drives the physical racer; never moves it.</summary>
    public sealed class CircuitReviewDriver
    {
        private readonly Vector3[] points;
        private readonly float[] curvature;
        private bool cooling;
        private bool demonstratedBurnout;
        public float RoadDistance { get; private set; }

        public CircuitReviewDriver(Vector3[] route)
        {
            points = route;
            curvature = new float[points.Length];
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i] - points[(i-1+points.Length)%points.Length];
                var b = points[(i+1)%points.Length] - points[i];
                curvature[i] = 2 * Vector3.Cross(a,b).magnitude / Mathf.Max(.001f,a.magnitude*b.magnitude*(a+b).magnitude);
            }
        }

        public GamepadState Sample(ArcadeWalkerController walker, Vector3 velocity)
        {
            var position = walker.transform.position; position.y = 0;
            var nearest = 0; var distance = float.PositiveInfinity;
            for (var i = 0; i < points.Length; i++)
            {
                var d = (points[i]-position).sqrMagnitude;
                if (d < distance) { distance = d; nearest = i; }
            }
            RoadDistance = Mathf.Sqrt(distance);
            var speed = walker.Speed;
            var lookAhead = 6 + speed * .32f;
            var target = nearest; var along = 0f;
            var targetSpeed = walker.Stats.BoostMaximumSpeed;
            for (var step = 0; step < points.Length && along < 65; step++)
            {
                var i = (nearest+step)%points.Length;
                var next = (i+1)%points.Length;
                var cornerSpeed = Mathf.Sqrt(21 / Mathf.Max(.001f, curvature[i]));
                // Preview braking distance; tight corners are slower, straights use full speed.
                targetSpeed = Mathf.Min(targetSpeed, Mathf.Sqrt(cornerSpeed*cornerSpeed + 2*25*Mathf.Max(0,along-5)));
                if (along < lookAhead) target = next;
                along += Vector3.Distance(points[i], points[next]);
            }
            var toTarget = points[target] - position;
            var angle = Vector3.SignedAngle(walker.transform.forward, toTarget, Vector3.up);
            velocity.y = 0;
            var slip = speed > 2 ? Vector3.SignedAngle(walker.transform.forward, velocity, Vector3.up) : 0;
            var yawRate = 2 * Mathf.Max(speed, 8) * Mathf.Sin((angle - slip * .65f) * Mathf.Deg2Rad)
                / Mathf.Max(3, toTarget.magnitude) * Mathf.Rad2Deg;
            var availableYaw = walker.Handling.steeringDegreesPerSecond * WalkerHandlingMath.SteeringScale(
                speed, walker.Handling.steeringFalloffSpeed, walker.Handling.topSpeedSteeringScale);
            var steer = Mathf.Clamp(yawRate / availableYaw, -1, 1);
            if (walker.HeatStage == SlipperHeatStage.BurnedOut) { demonstratedBurnout = true; cooling = true; }
            if (demonstratedBurnout && walker.Heat >= .88f) cooling = true;
            if (walker.Heat <= .25f) cooling = false;
            var boost = !cooling && targetSpeed > walker.Stats.MaximumSpeed + 4 && Mathf.Abs(angle) < 16;
            var desired = boost ? targetSpeed : Mathf.Min(targetSpeed, walker.Stats.MaximumSpeed);
            return new GamepadState
            {
                rightTrigger = speed < desired + .3f ? 1 : 0,
                leftTrigger = Mathf.Clamp01((speed-desired-1) / 5),
                leftStick = new Vector2(steer, 0)
            }.WithButton(GamepadButton.South, boost);
        }
    }
}
#endif

namespace GrannyRacer.Walker
{
    public static class WalkerHandlingMath
    {
        public static float SteeringScale(float speed, float fullSteeringSpeed, float topSpeedScale)
        {
            if (fullSteeringSpeed <= 0f || speed <= 0f)
            {
                return 1f;
            }

            var t = Clamp01(speed / fullSteeringSpeed);
            return 1f + (Clamp01(topSpeedScale) - 1f) * t;
        }

        public static float DriveInput(float throttle, float brake, float forwardSpeed)
        {
            throttle = Clamp01(throttle);
            brake = Clamp01(brake);
            return forwardSpeed > 0.5f ? throttle - brake : throttle - brake * 0.65f;
        }

        public static bool ShouldSkid(float forwardSpeed, float brake, float steer,
            float minimumSpeed, float minimumBrake, float minimumSteer)
        {
            return System.Math.Abs(forwardSpeed) >= minimumSpeed
                && brake >= minimumBrake
                && System.Math.Abs(steer) >= minimumSteer;
        }

        /// <summary>
        /// Steering used while drifting. The drift holds its own line, and the player's stick
        /// tightens it when steering into the drift or opens it when counter-steering. The
        /// result never crosses zero, so a drift cannot be steered inside out.
        /// </summary>
        public static float DriftSteer(int direction, float steer, float bias, float control)
        {
            if (direction == 0) return steer;
            var sign = direction < 0 ? -1f : 1f;
            var blended = sign * bias + Clamp(steer, -1f, 1f) * control;
            var clamped = Clamp(blended, -1f, 1f);
            return sign > 0f ? Clamp(clamped, 0f, 1f) : Clamp(clamped, -1f, 0f);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            if (value < minimum) return minimum;
            return value > maximum ? maximum : value;
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}

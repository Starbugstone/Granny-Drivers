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
        /// Steering used while drifting. The drift holds its own line at <paramref name="bias"/>,
        /// and the player's stick bends it from there.
        ///
        /// The two directions are deliberately not symmetric. Steering into the drift adds only
        /// a little, because the bias already has it turning hard — that is the arcade payoff,
        /// a drifted corner beats a gripped one. Counter-steering subtracts much more, so a
        /// player fighting the slide runs almost straight and can hold a drift down a straight
        /// to keep charging. It bottoms out at <paramref name="minimumHold"/> rather than zero:
        /// a drift never resolves to neutral, and never crosses through to the other lock.
        /// </summary>
        public static float DriftSteer(int direction, float steer, float bias, float inwardControl,
            float counterControl, float minimumHold)
        {
            if (direction == 0) return steer;
            var sign = direction < 0 ? -1f : 1f;
            var into = Clamp(steer, -1f, 1f) * sign;
            var hold = into >= 0f
                ? bias + into * inwardControl
                : bias + into * counterControl;
            return sign * Clamp(hold, Clamp01(minimumHold), 1f);
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

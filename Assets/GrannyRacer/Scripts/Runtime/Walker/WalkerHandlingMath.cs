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

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}

namespace GrannyRacer.Racing
{
    public static class CheckpointCrossing
    {
        public static bool Forward(float previousDistance, float currentDistance, float forwardSpeed)
            => previousDistance <= 0f && currentDistance > 0f && forwardSpeed > .1f;
    }
}

namespace GrannyRacer.Input
{
    public readonly struct RacerInputState
    {
        public RacerInputState(
            float throttle,
            float brake,
            float steer,
            bool boost,
            bool boostPressed,
            bool reset,
            bool pause,
            bool leftAttack,
            bool rightAttack)
        {
            Throttle = throttle;
            Brake = brake;
            Steer = steer;
            Boost = boost;
            BoostPressed = boostPressed;
            Reset = reset;
            Pause = pause;
            LeftAttack = leftAttack;
            RightAttack = rightAttack;
        }

        public float Throttle { get; }
        public float Brake { get; }
        public float Steer { get; }
        public bool Boost { get; }
        public bool BoostPressed { get; }
        public bool Reset { get; }
        public bool Pause { get; }
        public bool LeftAttack { get; }
        public bool RightAttack { get; }
    }
}

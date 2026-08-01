namespace GrannyRacer.Racing
{
    public sealed class RaceProgress
    {
        private readonly int checkpointCount;
        private readonly int lapTarget;

        public RaceProgress(int checkpointCount, int lapTarget, int firstExpectedCheckpoint = 1)
        {
            this.checkpointCount = checkpointCount < 2 ? 2 : checkpointCount;
            this.lapTarget = lapTarget < 1 ? 1 : lapTarget;
            ExpectedCheckpoint = Normalise(firstExpectedCheckpoint, this.checkpointCount);
        }

        public int CompletedLaps { get; private set; }
        public int ExpectedCheckpoint { get; private set; }
        public int LastCheckpoint { get; private set; }
        public bool IsFinished => CompletedLaps >= lapTarget;
        public int OrderedProgress => CompletedLaps * checkpointCount + LastCheckpoint;

        public bool TryPassCheckpoint(int checkpointIndex)
        {
            if (IsFinished || checkpointIndex != ExpectedCheckpoint) return false;

            LastCheckpoint = checkpointIndex;
            ExpectedCheckpoint = (ExpectedCheckpoint + 1) % checkpointCount;
            if (ExpectedCheckpoint == 1)
            {
                CompletedLaps++;
            }

            return true;
        }

        private static int Normalise(int value, int count)
        {
            var result = value % count;
            return result < 0 ? result + count : result;
        }
    }
}

namespace GrannyRacer.Racing
{
    public static class RacePositionMath
    {
        public static int GetPosition(int racerIndex, RaceProgress[] progressByRacer)
        {
            if (progressByRacer == null || racerIndex < 0 || racerIndex >= progressByRacer.Length) return 0;
            var position = 1;
            var ownProgress = progressByRacer[racerIndex].OrderedProgress;
            for (var i = 0; i < progressByRacer.Length; i++)
            {
                if (i != racerIndex && progressByRacer[i].OrderedProgress > ownProgress) position++;
            }

            return position;
        }
    }
}

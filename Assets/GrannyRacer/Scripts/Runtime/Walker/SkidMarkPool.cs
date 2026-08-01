namespace GrannyRacer.Walker
{
    /// <summary>
    /// Decides which of a slipper's skid ribbons to draw into.
    ///
    /// One <c>TrailRenderer</c> per slipper is not enough. A trail that is switched off keeps
    /// its points while they age out, so switching the same one back on for the next drift
    /// joins the two with a straight streak across the road — the walker's path between the
    /// skids, drawn as a skid. Each unbroken skid therefore gets its own ribbon, and a released
    /// ribbon is left alone to fade. Ribbons are only recycled once every other one is in use,
    /// oldest release first, so with enough of them a mark always gets its full fade.
    /// </summary>
    public sealed class SkidMarkPool
    {
        public const int NoRibbon = -1;

        /// <summary>Time each ribbon stopped emitting. Never used ribbons sort oldest.</summary>
        private readonly float[] releasedAt;

        private int active = NoRibbon;

        public SkidMarkPool(int count)
        {
            if (count < 1) count = 1;
            releasedAt = new float[count];
            for (var i = 0; i < count; i++) releasedAt[i] = float.NegativeInfinity;
        }

        public int Count => releasedAt.Length;

        /// <summary>The ribbon currently being drawn into, or <see cref="NoRibbon"/>.</summary>
        public int Active => active;

        /// <summary>
        /// Returns the ribbon to draw into. <paramref name="started"/> is true only on the frame
        /// a new ribbon is taken, which is when the caller must clear and reposition it before
        /// letting it emit.
        /// </summary>
        public int Acquire(float now, out bool started)
        {
            if (active != NoRibbon)
            {
                started = false;
                return active;
            }

            var oldest = 0;
            for (var i = 1; i < releasedAt.Length; i++)
            {
                if (releasedAt[i] < releasedAt[oldest]) oldest = i;
            }

            active = oldest;
            // Parked out of the running so an in-use ribbon can never be picked as the oldest.
            releasedAt[oldest] = float.PositiveInfinity;
            started = true;
            return oldest;
        }

        /// <summary>
        /// Stops drawing and leaves the ribbon to fade. Returns the ribbon that was released,
        /// or <see cref="NoRibbon"/> when nothing was being drawn.
        /// </summary>
        public int Release(float now)
        {
            if (active == NoRibbon) return NoRibbon;
            var released = active;
            releasedAt[released] = now;
            active = NoRibbon;
            return released;
        }

        /// <summary>
        /// Returns a ribbon whose fade has certainly finished, so the caller can empty it, or
        /// <see cref="NoRibbon"/>.
        ///
        /// A TrailRenderer only ages its points while it is being rendered, so a mark that falls
        /// behind the camera stops fading and can pop back into view long after its time is up.
        /// Emptying released ribbons on our own clock turns the fade time into a guarantee.
        /// </summary>
        public int TryRecycle(float now, float fadeSeconds)
        {
            for (var i = 0; i < releasedAt.Length; i++)
            {
                // Infinite either way means "not fading": positive is the ribbon in use,
                // negative is one already back in the free pool.
                if (float.IsInfinity(releasedAt[i])) continue;
                if (now - releasedAt[i] < fadeSeconds) continue;
                releasedAt[i] = float.NegativeInfinity;
                return i;
            }

            return NoRibbon;
        }

        public void Reset()
        {
            active = NoRibbon;
            for (var i = 0; i < releasedAt.Length; i++) releasedAt[i] = float.NegativeInfinity;
        }
    }
}

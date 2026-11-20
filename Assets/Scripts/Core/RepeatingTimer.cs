namespace ChromaStrike.Core
{
    /// <summary>
    /// Fires every N seconds. If a frame is longer than the interval it reports several ticks,
    /// so a lag spike can never swallow a periodic spawn.
    /// </summary>
    public sealed class RepeatingTimer
    {
        public float Interval { get; }
        private float accumulated;

        public RepeatingTimer(float interval) => Interval = interval > 0.0001f ? interval : 0.0001f;

        /// <returns>How many intervals elapsed during this step.</returns>
        public int Advance(float deltaTime)
        {
            accumulated += deltaTime;
            int ticks = 0;
            while (accumulated >= Interval)
            {
                accumulated -= Interval;
                ticks++;
            }
            return ticks;
        }

        public void Reset() => accumulated = 0f;
    }
}

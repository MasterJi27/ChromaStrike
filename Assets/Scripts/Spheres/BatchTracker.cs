using System;

namespace ChromaStrike.Spheres
{
    /// <summary>
    /// Implements the "every 10 destroyed spheres" rule.
    /// It remembers which colours were destroyed since the last batch. When the tenth sphere is
    /// registered it hands back that exact colour mix, so the replacement batch has the same ratio
    /// (example: 5 red, 2 green, 3 blue destroyed -> 5 red, 2 green, 3 blue spawned).
    /// </summary>
    public sealed class BatchTracker
    {
        private ColorTally pending;

        public int BatchSize { get; }
        public int Progress => pending.Total;

        /// <summary>Raised with (destroyedSoFar, batchSize) whenever the counter moves.</summary>
        public event Action<int, int> ProgressChanged;

        public BatchTracker(int batchSize)
        {
            if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
            BatchSize = batchSize;
        }

        /// <summary>Registers one destroyed sphere. Returns true when a full batch is ready.</summary>
        public bool TryRegister(SphereColor color, out ColorTally batch)
        {
            pending = pending.With(color);
            ProgressChanged?.Invoke(pending.Total, BatchSize);

            if (pending.Total < BatchSize)
            {
                batch = default;
                return false;
            }

            batch = pending;
            pending = default;
            ProgressChanged?.Invoke(0, BatchSize);
            return true;
        }

        public void Reset()
        {
            pending = default;
            ProgressChanged?.Invoke(0, BatchSize);
        }
    }
}

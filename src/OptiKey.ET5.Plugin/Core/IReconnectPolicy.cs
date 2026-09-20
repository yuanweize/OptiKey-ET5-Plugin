using System;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Defines reconnection delay calculation and retry state tracking.
    /// </summary>
    public interface IReconnectPolicy
    {
        /// <summary>
        /// Calculates the next delay in milliseconds based on current attempt number.
        /// </summary>
        int GetNextDelayMilliseconds(int attemptNumber);

        /// <summary>
        /// Resets the retry counter.
        /// </summary>
        void Reset();
    }

    /// <summary>
    /// Exponential backoff with bounded cap and randomized jitter (ADR-005).
    /// Prevents bus contention and CPU thrashing during prolonged disconnects.
    /// </summary>
    public class ExponentialBackoffReconnectPolicy : IReconnectPolicy
    {
        private readonly int baseDelayMs;
        private readonly int maxDelayMs;
        private readonly int maxJitterMs;
        private readonly Random random;

        public ExponentialBackoffReconnectPolicy(
            int baseDelayMs = 500,
            int maxDelayMs = 10000,
            int maxJitterMs = 250,
            int? seed = null)
        {
            if (baseDelayMs <= 0) throw new ArgumentOutOfRangeException(nameof(baseDelayMs));
            if (maxDelayMs < baseDelayMs) throw new ArgumentOutOfRangeException(nameof(maxDelayMs));
            if (maxJitterMs < 0) throw new ArgumentOutOfRangeException(nameof(maxJitterMs));

            this.baseDelayMs = baseDelayMs;
            this.maxDelayMs = maxDelayMs;
            this.maxJitterMs = maxJitterMs;
            this.random = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public int GetNextDelayMilliseconds(int attemptNumber)
        {
            if (attemptNumber < 0) attemptNumber = 0;

            // Cap exponent to prevent 32-bit integer overflow
            int exponent = Math.Min(attemptNumber, 10);
            long rawDelay = (long)baseDelayMs * (1L << exponent);
            int clampedDelay = (int)Math.Min(rawDelay, (long)maxDelayMs);

            int jitter;
            lock (random)
            {
                jitter = maxJitterMs > 0 ? random.Next(0, maxJitterMs) : 0;
            }

            return clampedDelay + jitter;
        }

        public void Reset()
        {
            // Stateless policy, nothing to mutate, ready for new attempts
        }
    }
}

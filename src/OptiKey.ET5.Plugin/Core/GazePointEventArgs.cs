using System;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Event arguments containing normalized raw gaze coordinates from the sensor.
    /// </summary>
    public class GazePointEventArgs : EventArgs
    {
        /// <summary>
        /// Normalized X coordinate in range [0.0, 1.0] (0 = left, 1 = right).
        /// </summary>
        public float NormalizedX { get; }

        /// <summary>
        /// Normalized Y coordinate in range [0.0, 1.0] (0 = top, 1 = bottom).
        /// </summary>
        public float NormalizedY { get; }

        /// <summary>
        /// Hardware sensor timestamp in microseconds.
        /// </summary>
        public long TimestampMicroseconds { get; }

        /// <summary>
        /// System UTC time corresponding to the point generation.
        /// </summary>
        public DateTime UtcTimestamp { get; }

        /// <summary>
        /// True if eye tracking confidence/validity was confirmed by hardware.
        /// </summary>
        public bool IsValid { get; }

        public GazePointEventArgs(float x, float y, long timestampUs, bool isValid, DateTime? utcTimestamp = null)
        {
            NormalizedX = x;
            NormalizedY = y;
            TimestampMicroseconds = timestampUs;
            IsValid = isValid;
            UtcTimestamp = utcTimestamp ?? DateTime.UtcNow;
        }
    }
}

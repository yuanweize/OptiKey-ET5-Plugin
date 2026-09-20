using System;

namespace OptiKey.ET5.Plugin.State
{
    /// <summary>
    /// Lifecycle states for the eye-tracking point service (ADR-005).
    /// </summary>
    public enum GazeServiceState
    {
        /// <summary>
        /// Initial state. Parameterless construction complete. No background threads or hardware allocated.
        /// </summary>
        Created,

        /// <summary>
        /// Locating runtime, initializing API context, enumerating devices.
        /// </summary>
        Starting,

        /// <summary>
        /// Device connected, gaze stream active and delivering points.
        /// </summary>
        Connected,

        /// <summary>
        /// Device disconnected, sleeping, or stream interrupted. Actively retrying with backoff.
        /// </summary>
        Reconnecting,

        /// <summary>
        /// Shutdown requested. Cleaning up stream and background threads.
        /// </summary>
        Stopping,

        /// <summary>
        /// Idle/Stopped state. Cleaned up, ready to restart if requested.
        /// </summary>
        Stopped,

        /// <summary>
        /// Terminal state. All resources released. No further operations permitted.
        /// </summary>
        Disposed
    }

    /// <summary>
    /// Structured error information associated with lifecycle transitions.
    /// </summary>
    public class GazeServiceError
    {
        public string ErrorCode { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public DateTime TimestampUtc { get; }

        public GazeServiceError(string errorCode, string message, Exception exception = null)
        {
            ErrorCode = errorCode ?? "UNKNOWN";
            Message = message ?? string.Empty;
            Exception = exception;
            TimestampUtc = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"[{TimestampUtc:yyyy-MM-dd HH:mm:ss.fffZ}] ({ErrorCode}) {Message}";
        }
    }
}

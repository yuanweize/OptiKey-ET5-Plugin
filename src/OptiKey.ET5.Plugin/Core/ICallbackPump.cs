using System;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Abstraction for native callback processing strategy.
    /// Decouples the gaze provider from the specific mechanism used to
    /// receive callbacks (wait+process, polling, or out-of-process IPC).
    ///
    /// All implementations must honor the RequestStop() / Join(timeout) lifecycle contract.
    /// If Join(timeout) times out (e.g. worker is blocked in an unboundable native wait):
    /// - The pump enters the <see cref="CallbackPumpState.TimedOut"/> state.
    /// - A critical warning is logged.
    /// - Underlying native handles MUST NOT be destroyed to avoid use-after-free crashes.
    /// </summary>
    public interface ICallbackPump : IDisposable
    {
        /// <summary>
        /// Current pump state.
        /// </summary>
        CallbackPumpState State { get; }

        /// <summary>
        /// Start the callback processing loop on a background thread.
        /// Must be idempotent: calling Start() when already running returns immediately.
        /// </summary>
        void Start();

        /// <summary>
        /// Signal the pump to stop processing. Non-blocking.
        /// After this call, the pump should exit its loop at the next safe point.
        /// </summary>
        void RequestStop();

        /// <summary>
        /// Block until the pump has fully stopped, or until the timeout expires.
        /// Returns true if the pump stopped cleanly within the timeout; false if it timed out.
        /// </summary>
        bool Join(TimeSpan timeout);

        /// <summary>
        /// Raised when an unhandled exception occurs inside the pump worker loop.
        /// </summary>
        event EventHandler<Exception> PumpError;
    }

    /// <summary>
    /// Lifecycle states for a callback pump.
    /// </summary>
    public enum CallbackPumpState
    {
        /// <summary>Created but not started.</summary>
        Idle = 0,

        /// <summary>Actively processing callbacks on a worker thread.</summary>
        Running = 1,

        /// <summary>Stop has been requested; waiting for worker loop to exit.</summary>
        StopRequested = 2,

        /// <summary>Worker thread terminated cleanly and pump has stopped.</summary>
        Stopped = 3,

        /// <summary>
        /// Join timed out. The worker thread is still alive and may be stuck in native wait.
        /// Native resources MUST NOT be freed while in this state to prevent use-after-free.
        /// </summary>
        TimedOut = 4,

        /// <summary>Worker loop terminated due to an unhandled exception.</summary>
        Faulted = 5,

        /// <summary>Disposed. Cannot be reused.</summary>
        Disposed = 6
    }
}

using System;

namespace OptiKey.ET5.Plugin.State
{
    /// <summary>
    /// Thread-safe state machine governing the life cycle of the ET5 point service (ADR-005).
    /// </summary>
    public class GazeServiceStateMachine
    {
        private readonly object syncLock = new object();
        private GazeServiceState currentState;
        private GazeServiceError lastError;

        public event EventHandler<GazeServiceState> StateChanged;
        public event EventHandler<GazeServiceError> ErrorOccurred;

        public GazeServiceStateMachine()
        {
            currentState = GazeServiceState.Created;
        }

        public GazeServiceState CurrentState
        {
            get
            {
                lock (syncLock)
                {
                    return currentState;
                }
            }
        }

        public GazeServiceError LastError
        {
            get
            {
                lock (syncLock)
                {
                    return lastError;
                }
            }
        }

        public bool IsRunning
        {
            get
            {
                lock (syncLock)
                {
                    return currentState == GazeServiceState.Connected ||
                           currentState == GazeServiceState.Starting ||
                           currentState == GazeServiceState.Reconnecting;
                }
            }
        }

        /// <summary>
        /// Attempts to transition to the new state according to the valid state flow rules.
        /// </summary>
        public bool TryTransition(GazeServiceState newState, GazeServiceError error = null)
        {
            GazeServiceState oldState;
            bool transitioned = false;

            lock (syncLock)
            {
                if (currentState == GazeServiceState.Disposed)
                {
                    // Terminal state. Cannot transition out.
                    return false;
                }

                if (currentState == newState)
                {
                    // Update error if supplied
                    if (error != null)
                    {
                        lastError = error;
                    }
                    return true;
                }

                if (!IsValidTransition(currentState, newState))
                {
                    return false;
                }

                oldState = currentState;
                currentState = newState;
                if (error != null)
                {
                    lastError = error;
                }
                transitioned = true;
            }

            if (transitioned)
            {
                if (error != null)
                {
                    ErrorOccurred?.Invoke(this, error);
                }
                StateChanged?.Invoke(this, newState);
            }

            return transitioned;
        }

        public void ForceDisposed()
        {
            lock (syncLock)
            {
                currentState = GazeServiceState.Disposed;
            }
            StateChanged?.Invoke(this, GazeServiceState.Disposed);
        }

        private static bool IsValidTransition(GazeServiceState from, GazeServiceState to)
        {
            // Disposed is accessible from any state
            if (to == GazeServiceState.Disposed)
            {
                return true;
            }

            switch (from)
            {
                case GazeServiceState.Created:
                    return to == GazeServiceState.Starting || to == GazeServiceState.Stopped;

                case GazeServiceState.Starting:
                    return to == GazeServiceState.Connected ||
                           to == GazeServiceState.Reconnecting ||
                           to == GazeServiceState.Stopping ||
                           to == GazeServiceState.Stopped;

                case GazeServiceState.Connected:
                    return to == GazeServiceState.Reconnecting ||
                           to == GazeServiceState.Stopping ||
                           to == GazeServiceState.Stopped;

                case GazeServiceState.Reconnecting:
                    return to == GazeServiceState.Connected ||
                           to == GazeServiceState.Stopping ||
                           to == GazeServiceState.Stopped;

                case GazeServiceState.Stopping:
                    return to == GazeServiceState.Stopped;

                case GazeServiceState.Stopped:
                    return to == GazeServiceState.Starting;

                case GazeServiceState.Disposed:
                    return false;

                default:
                    return false;
            }
        }
    }
}

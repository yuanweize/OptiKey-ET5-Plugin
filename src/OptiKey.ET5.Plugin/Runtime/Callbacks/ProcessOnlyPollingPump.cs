using System;
using System.Threading;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Alternative callback pump using tobii_device_process_callbacks in a polling loop
    /// with a small delay, completely bypassing tobii_wait_for_callbacks.
    ///
    /// This eliminates the unbounded blocking risk of WaitForCallbacks because the poll
    /// delay is interruptible via an event signal.
    /// </summary>
    public sealed class ProcessOnlyPollingPump : ICallbackPump
    {
        private readonly Func<tobii_error_t> processFunc;
        private readonly Action<tobii_error_t> streamErrorCallback;
        private readonly int pollIntervalMs;
        private readonly IPluginLogger logger;

        private readonly object stateLock = new object();
        private readonly ManualResetEventSlim stopEvent = new ManualResetEventSlim(false);
        private Thread workerThread;
        private CallbackPumpState state = CallbackPumpState.Idle;

        public CallbackPumpState State
        {
            get
            {
                lock (stateLock)
                {
                    return state;
                }
            }
            private set
            {
                lock (stateLock)
                {
                    state = value;
                }
            }
        }

        public event EventHandler<Exception> PumpError;

        public ProcessOnlyPollingPump(
            ITobiiRuntime runtime,
            Action<tobii_error_t> streamErrorCallback = null,
            int pollIntervalMs = 5,
            IPluginLogger logger = null)
            : this(
                runtime != null ? runtime.ProcessCallbacks : (Func<tobii_error_t>)null,
                streamErrorCallback,
                pollIntervalMs,
                logger)
        {
        }

        public ProcessOnlyPollingPump(
            Func<tobii_error_t> processFunc,
            Action<tobii_error_t> streamErrorCallback = null,
            int pollIntervalMs = 5,
            IPluginLogger logger = null)
        {
            this.processFunc = processFunc ?? throw new ArgumentNullException(nameof(processFunc));
            this.streamErrorCallback = streamErrorCallback;
            this.pollIntervalMs = Math.Max(1, pollIntervalMs);
            this.logger = logger;
        }

        public void Start()
        {
            lock (stateLock)
            {
                if (state == CallbackPumpState.Disposed)
                {
                    throw new ObjectDisposedException(nameof(ProcessOnlyPollingPump));
                }

                if (state == CallbackPumpState.Running)
                {
                    return;
                }

                stopEvent.Reset();
                state = CallbackPumpState.Running;

                workerThread = new Thread(Loop)
                {
                    Name = "TobiiPollingPumpWorker",
                    IsBackground = true
                };
                workerThread.Start();
                logger?.Debug($"ProcessOnlyPollingPump worker thread started (poll interval: {pollIntervalMs}ms).");
            }
        }

        public void RequestStop()
        {
            lock (stateLock)
            {
                if (state == CallbackPumpState.Running)
                {
                    state = CallbackPumpState.StopRequested;
                }
                stopEvent.Set();
                logger?.Debug("ProcessOnlyPollingPump stop requested.");
            }
        }

        public bool Join(TimeSpan timeout)
        {
            Thread t;
            lock (stateLock)
            {
                t = workerThread;
            }

            if (t == null || !t.IsAlive)
            {
                lock (stateLock)
                {
                    if (state != CallbackPumpState.Faulted && state != CallbackPumpState.Disposed)
                    {
                        state = CallbackPumpState.Stopped;
                    }
                }
                return true;
            }

            bool joined = t.Join(timeout);

            lock (stateLock)
            {
                if (joined)
                {
                    if (state != CallbackPumpState.Faulted && state != CallbackPumpState.Disposed)
                    {
                        state = CallbackPumpState.Stopped;
                    }
                    logger?.Debug("ProcessOnlyPollingPump stopped cleanly.");
                    return true;
                }
                else
                {
                    if (state != CallbackPumpState.Disposed)
                    {
                        state = CallbackPumpState.TimedOut;
                    }
                    logger?.Error($"ProcessOnlyPollingPump worker did not exit within {timeout.TotalSeconds:F1}s.");
                    return false;
                }
            }
        }

        private void Loop()
        {
            try
            {
                while (!stopEvent.IsSet)
                {
                    tobii_error_t processResult;
                    try
                    {
                        processResult = processFunc();
                    }
                    catch (Exception ex)
                    {
                        logger?.Error("Exception in ProcessCallbacks delegate.", ex);
                        State = CallbackPumpState.Faulted;
                        PumpError?.Invoke(this, ex);
                        return;
                    }

                    if (stopEvent.IsSet)
                    {
                        break;
                    }

                    if (processResult != tobii_error_t.TOBII_ERROR_NO_ERROR &&
                        processResult != tobii_error_t.TOBII_ERROR_TIMED_OUT)
                    {
                        streamErrorCallback?.Invoke(processResult);
                    }

                    // Interruptible poll wait
                    stopEvent.Wait(pollIntervalMs);
                }

                lock (stateLock)
                {
                    if (state != CallbackPumpState.Faulted && state != CallbackPumpState.TimedOut && state != CallbackPumpState.Disposed)
                    {
                        state = CallbackPumpState.Stopped;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error("Unhandled exception in polling pump loop.", ex);
                State = CallbackPumpState.Faulted;
                PumpError?.Invoke(this, ex);
            }
        }

        public void Dispose()
        {
            Thread threadToJoin;
            lock (stateLock)
            {
                if (state == CallbackPumpState.Disposed)
                {
                    return;
                }

                RequestStop();
                threadToJoin = workerThread;
                workerThread = null;
            }

            // Invariant: Never hold stateLock while joining the worker thread (CONC-01)
            if (threadToJoin != null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
            {
                threadToJoin.Join(TimeSpan.FromMilliseconds(500));
            }

            lock (stateLock)
            {
                if (threadToJoin == null || !threadToJoin.IsAlive)
                {
                    state = CallbackPumpState.Disposed;
                    try
                    {
                        stopEvent.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger?.Debug($"Exception disposing stopEvent: {ex.Message}");
                    }
                }
                else
                {
                    state = CallbackPumpState.TimedOut;
                    logger?.Warn("Worker thread did not terminate within dispose timeout; retaining stopEvent to prevent ObjectDisposedException.");
                }
            }
        }
    }
}

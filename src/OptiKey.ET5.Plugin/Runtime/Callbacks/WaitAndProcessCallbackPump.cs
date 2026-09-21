using System;
using System.Threading;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Classic callback pump using tobii_wait_for_callbacks followed by
    /// tobii_device_process_callbacks.
    ///
    /// Implements bounded shutdown: if Join(timeout) times out (the worker thread
    /// is stuck in native wait), the pump transitions to <see cref="CallbackPumpState.TimedOut"/>
    /// and logs a critical error. The caller MUST NOT release native resources while
    /// in this state.
    /// </summary>
    public sealed class WaitAndProcessCallbackPump : ICallbackPump
    {
        private readonly Func<tobii_error_t> waitFunc;
        private readonly Func<tobii_error_t> processFunc;
        private readonly Action<tobii_error_t> streamErrorCallback;
        private readonly IPluginLogger logger;

        private readonly object stateLock = new object();
        private Thread workerThread;
        private volatile bool stopRequested;
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

        public WaitAndProcessCallbackPump(
            ITobiiRuntime runtime,
            Action<tobii_error_t> streamErrorCallback = null,
            IPluginLogger logger = null)
            : this(
                runtime != null ? runtime.WaitForCallbacks : (Func<tobii_error_t>)null,
                runtime != null ? runtime.ProcessCallbacks : (Func<tobii_error_t>)null,
                streamErrorCallback,
                logger)
        {
        }

        public WaitAndProcessCallbackPump(
            Func<tobii_error_t> waitFunc,
            Func<tobii_error_t> processFunc,
            Action<tobii_error_t> streamErrorCallback = null,
            IPluginLogger logger = null)
        {
            this.waitFunc = waitFunc ?? throw new ArgumentNullException(nameof(waitFunc));
            this.processFunc = processFunc ?? throw new ArgumentNullException(nameof(processFunc));
            this.streamErrorCallback = streamErrorCallback;
            this.logger = logger;
        }

        public void Start()
        {
            lock (stateLock)
            {
                if (state == CallbackPumpState.Disposed)
                {
                    throw new ObjectDisposedException(nameof(WaitAndProcessCallbackPump));
                }

                if (state == CallbackPumpState.Running)
                {
                    return;
                }

                stopRequested = false;
                state = CallbackPumpState.Running;

                workerThread = new Thread(Loop)
                {
                    Name = "TobiiWaitAndProcessPump",
                    IsBackground = true
                };
                workerThread.Start();
                logger?.Debug("WaitAndProcessCallbackPump worker thread started.");
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
                stopRequested = true;
                logger?.Debug("WaitAndProcessCallbackPump stop requested.");
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
                    logger?.Debug("WaitAndProcessCallbackPump stopped cleanly.");
                    return true;
                }
                else
                {
                    if (state != CallbackPumpState.Disposed)
                    {
                        state = CallbackPumpState.TimedOut;
                    }
                    logger?.Error(
                        $"CRITICAL: WaitAndProcessCallbackPump worker did not exit within {timeout.TotalSeconds:F1}s. " +
                        "Native thread is potentially stuck in tobii_wait_for_callbacks. " +
                        "Refusing to release native handles to prevent use-after-free.");
                    return false;
                }
            }
        }

        private void Loop()
        {
            try
            {
                while (!stopRequested)
                {
                    tobii_error_t waitResult;
                    try
                    {
                        waitResult = waitFunc();
                    }
                    catch (Exception ex)
                    {
                        logger?.Error("Exception in native WaitForCallbacks delegate.", ex);
                        State = CallbackPumpState.Faulted;
                        PumpError?.Invoke(this, ex);
                        return;
                    }

                    if (stopRequested)
                    {
                        break;
                    }

                    if (waitResult == tobii_error_t.TOBII_ERROR_NO_ERROR)
                    {
                        tobii_error_t processResult;
                        try
                        {
                            processResult = processFunc();
                        }
                        catch (Exception ex)
                        {
                            logger?.Error("Exception in native ProcessCallbacks delegate.", ex);
                            State = CallbackPumpState.Faulted;
                            PumpError?.Invoke(this, ex);
                            return;
                        }

                        if (processResult != tobii_error_t.TOBII_ERROR_NO_ERROR)
                        {
                            streamErrorCallback?.Invoke(processResult);
                        }
                    }
                    else
                    {
                        streamErrorCallback?.Invoke(waitResult);
                    }
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
                logger?.Error("Unhandled exception in pump loop.", ex);
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
                state = CallbackPumpState.Disposed;
            }

            // Invariant: Never hold stateLock while joining the worker thread (CONC-01)
            if (threadToJoin != null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
            {
                threadToJoin.Join(TimeSpan.FromMilliseconds(500));
            }
        }
    }
}

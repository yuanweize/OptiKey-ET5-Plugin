using System;
using System.Collections.Generic;
using System.Threading;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Diagnostics;
using OptiKey.ET5.Plugin.State;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Production eye-gaze provider interfacing with Tobii hardware via ITobiiRuntime (ADR-005).
    /// </summary>
    public class TobiiGazeProvider : IGazeProvider
    {
        private readonly ITobiiRuntime runtime;
        private readonly IReconnectPolicy reconnectPolicy;
        private readonly IPluginLogger logger;
        private readonly GazeServiceStateMachine stateMachine;

        private Thread workerThread;
        private CancellationTokenSource cts;
        private readonly object lifecycleLock = new object();
        private tobii_gaze_point_callback_t nativeGazeCallback; // Prevent GC collection of delegate

        public event EventHandler<GazePointEventArgs> GazePointAvailable;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<bool> ConnectionStatusChanged;

        public bool IsConnected => stateMachine.CurrentState == GazeServiceState.Connected;
        public GazeServiceStateMachine StateMachine => stateMachine;

        public TobiiGazeProvider(
            ITobiiRuntime runtime = null,
            IReconnectPolicy reconnectPolicy = null,
            IPluginLogger logger = null,
            GazeServiceStateMachine stateMachine = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiGazeProvider));
            this.runtime = runtime ?? new TobiiNativeRuntime(logger: this.logger);
            this.reconnectPolicy = reconnectPolicy ?? new ExponentialBackoffReconnectPolicy();
            this.stateMachine = stateMachine ?? new GazeServiceStateMachine();

            // Pin delegate to instance field to prevent unmanaged callback crash
            this.nativeGazeCallback = OnNativeGazePoint;
        }

        public void Start()
        {
            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed)
                {
                    throw new ObjectDisposedException(nameof(TobiiGazeProvider));
                }

                if (workerThread != null && workerThread.IsAlive)
                {
                    logger.Debug("Worker thread already running.");
                    return;
                }

                cts = new CancellationTokenSource();
                stateMachine.TryTransition(GazeServiceState.Starting);

                workerThread = new Thread(WorkerLoop)
                {
                    Name = "TobiiGazeProviderWorker",
                    IsBackground = true
                };
                workerThread.Start();
                logger.Info("Tobii gaze provider worker thread started.");
            }
        }

        public void Stop()
        {
            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed || stateMachine.CurrentState == GazeServiceState.Stopped)
                {
                    return;
                }

                logger.Info("Stopping Tobii gaze provider...");
                stateMachine.TryTransition(GazeServiceState.Stopping);

                if (cts != null)
                {
                    cts.Cancel();
                }

                if (workerThread != null && workerThread.IsAlive && workerThread != Thread.CurrentThread)
                {
                    workerThread.Join();
                    workerThread = null;
                }

                try
                {
                    runtime.UnsubscribeGaze();
                    runtime.DisconnectDevice();
                }
                catch (Exception ex)
                {
                    logger.Warn($"Exception during stop cleanup: {ex.Message}");
                }

                stateMachine.TryTransition(GazeServiceState.Stopped);
                ConnectionStatusChanged?.Invoke(this, false);
                logger.Info("Tobii gaze provider stopped successfully.");
            }
        }

        private void WorkerLoop()
        {
            int reconnectAttempt = 0;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (stateMachine.CurrentState == GazeServiceState.Starting ||
                        stateMachine.CurrentState == GazeServiceState.Reconnecting)
                    {
                        bool connected = TryEstablishConnection();
                        if (connected)
                        {
                            reconnectAttempt = 0;
                            reconnectPolicy.Reset();
                            stateMachine.TryTransition(GazeServiceState.Connected);
                            ConnectionStatusChanged?.Invoke(this, true);
                            logger.Info("Tobii Eye Tracker connected and streaming gaze points.");
                        }
                        else
                        {
                            reconnectAttempt++;
                            int delayMs = reconnectPolicy.GetNextDelayMilliseconds(reconnectAttempt);
                            stateMachine.TryTransition(GazeServiceState.Reconnecting,
                                new GazeServiceError("DISCONNECTED", $"Attempt {reconnectAttempt} failed. Retrying in {delayMs}ms."));

                            ConnectionStatusChanged?.Invoke(this, false);

                            if (WaitOrCancel(delayMs, cts.Token))
                            {
                                break;
                            }
                            continue;
                        }
                    }

                    if (stateMachine.CurrentState == GazeServiceState.Connected)
                    {
                        // Event-driven callback wait (ADR-005)
                        var waitError = runtime.WaitForCallbacks();

                        if (cts.Token.IsCancellationRequested)
                        {
                            break;
                        }

                        if (waitError == tobii_error_t.TOBII_ERROR_NO_ERROR)
                        {
                            var processError = runtime.ProcessCallbacks();
                            if (processError != tobii_error_t.TOBII_ERROR_NO_ERROR)
                            {
                                HandleStreamError(processError);
                            }
                        }
                        else
                        {
                            HandleStreamError(waitError);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.Error("Unexpected exception in Tobii worker loop.", ex);
                    ErrorOccurred?.Invoke(this, ex);

                    stateMachine.TryTransition(GazeServiceState.Reconnecting,
                        new GazeServiceError("EXCEPTION", ex.Message, ex));

                    if (WaitOrCancel(1000, cts.Token))
                    {
                        break;
                    }
                }
            }

            logger.Debug("Worker loop exited.");
        }

        private bool TryEstablishConnection()
        {
            if (!runtime.Initialize())
            {
                return false;
            }

            List<string> deviceUrls;
            if (!runtime.EnumerateDevices(out deviceUrls) || deviceUrls == null || deviceUrls.Count == 0)
            {
                logger.Debug("No Tobii devices found on local system.");
                return false;
            }

            logger.Warn("Tobii device identity is not verified; refusing to bind to an unverified device.");
            return false;
        }

        private void HandleStreamError(tobii_error_t error)
        {
            logger.Warn($"Tobii stream error: {error}. Transitioning to Reconnecting.");
            stateMachine.TryTransition(GazeServiceState.Reconnecting,
                new GazeServiceError(error.ToString(), runtime.GetLastErrorDescription()));

            ConnectionStatusChanged?.Invoke(this, false);

            // Attempt reconnect through native runtime first
            if (!runtime.ReconnectDevice())
            {
                runtime.UnsubscribeGaze();
                runtime.DisconnectDevice();
            }
        }

        private void OnNativeGazePoint(ref tobii_gaze_point_t gazePoint, IntPtr userData)
        {
            if (cts != null && cts.Token.IsCancellationRequested)
            {
                return;
            }

            bool isValid = gazePoint.validity == tobii_validity_t.TOBII_VALIDITY_VALID;
            var args = new GazePointEventArgs(
                gazePoint.position_x,
                gazePoint.position_y,
                gazePoint.timestamp_us,
                isValid);

            GazePointAvailable?.Invoke(this, args);
        }

        private static bool WaitOrCancel(int milliseconds, CancellationToken token)
        {
            return token.WaitHandle.WaitOne(milliseconds);
        }

        public void Dispose()
        {
            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed)
                {
                    return;
                }

                Stop();
                stateMachine.ForceDisposed();

                if (cts != null)
                {
                    cts.Dispose();
                    cts = null;
                }

                runtime.Dispose();
                logger.Info("TobiiGazeProvider disposed.");
            }
        }
    }
}

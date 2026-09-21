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
    ///
    /// Supports two explicit modes:
    ///   STRICT MODE (default): refuses to bind when device identity is unverified.
    ///   DEVELOPER TEST MODE (opt-in): permits controlled testing with an explicitly
    ///     selected device index/URL. Never auto-selects the first device.
    /// </summary>
    public class TobiiGazeProvider : IGazeProvider
    {
        private readonly ITobiiRuntime runtime;
        private readonly IReconnectPolicy reconnectPolicy;
        private readonly IPluginLogger logger;
        private readonly GazeServiceStateMachine stateMachine;

        private PluginConfiguration configuration;
        private Thread workerThread;
        private CancellationTokenSource cts;
        private readonly object lifecycleLock = new object();
        private tobii_gaze_point_callback_t nativeGazeCallback; // Prevent GC collection of delegate

        public event EventHandler<GazePointEventArgs> GazePointAvailable;
        public event EventHandler<Exception> ErrorOccurred;
        public event EventHandler<bool> ConnectionStatusChanged;

        public bool IsConnected => stateMachine.CurrentState == GazeServiceState.Connected;
        public GazeServiceStateMachine StateMachine => stateMachine;

        private readonly TimeSpan stopTimeout;

        public TobiiGazeProvider(
            ITobiiRuntime runtime = null,
            IReconnectPolicy reconnectPolicy = null,
            IPluginLogger logger = null,
            GazeServiceStateMachine stateMachine = null,
            PluginConfiguration configuration = null,
            TimeSpan? stopTimeout = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiGazeProvider));
            this.runtime = runtime ?? new TobiiNativeRuntime(logger: this.logger);
            this.reconnectPolicy = reconnectPolicy ?? new ExponentialBackoffReconnectPolicy();
            this.stateMachine = stateMachine ?? new GazeServiceStateMachine();

            // Configuration is loaded lazily on first Start() if not injected,
            // to preserve the parameterless ET5PointService constructor contract.
            this.configuration = configuration;
            this.stopTimeout = stopTimeout ?? TimeSpan.FromSeconds(2);

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

                // Lazy configuration load (deferred from constructor)
                if (configuration == null)
                {
                    configuration = PluginConfiguration.Load(logger);
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
                    bool joined = workerThread.Join(stopTimeout);
                    if (!joined)
                    {
                        logger.Error(
                            $"CRITICAL: Worker thread did not terminate within {stopTimeout.TotalSeconds:F1}s. " +
                            "Native thread is potentially stuck in tobii_wait_for_callbacks. " +
                            "Aborting native handle cleanup to prevent use-after-free.");

                        stateMachine.TryTransition(GazeServiceState.Error,
                            new GazeServiceError("STUCK_WORKER", "Worker thread failed to terminate during Stop()."));
                        ConnectionStatusChanged?.Invoke(this, false);
                        return;
                    }
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
                ErrorOccurred?.Invoke(this, ET5PluginException.RuntimeNotFound());
                return false;
            }

            List<string> deviceUrls;
            if (!runtime.EnumerateDevices(out deviceUrls) || deviceUrls == null || deviceUrls.Count == 0)
            {
                logger.Debug("No Tobii devices found on local system.");
                ErrorOccurred?.Invoke(this, ET5PluginException.DeviceNotFound());
                return false;
            }

            logger.Info($"Enumerated {deviceUrls.Count} Tobii device candidate(s).");

            // --- STRICT MODE (default) ---
            if (!configuration.AllowUnverifiedTobiiDevice)
            {
                logger.Warn("Strict mode: device identity is not verified; refusing to bind.");
                ErrorOccurred?.Invoke(this, ET5PluginException.DeviceIdentityUnknown());
                return false;
            }

            // --- DEVELOPER TEST MODE (explicit opt-in) ---
            logger.Warn("*** DEVELOPER TEST MODE ACTIVE ***");
            logger.Warn("Device identity is NOT verified. NOT FOR PRODUCTION USE.");

            string selectedUrl = ResolveDeviceUrl(deviceUrls);
            if (selectedUrl == null)
            {
                logger.Error("Developer mode: no device index or URL configured. " +
                    "Set PreferredDeviceIndex or PreferredDeviceUrl to select a device.");
                ErrorOccurred?.Invoke(this, new ET5PluginException(
                    ET5ErrorCode.DeviceIdentityUnknown,
                    "Developer mode requires explicit device selection.",
                    "AllowUnverifiedTobiiDevice is true but no PreferredDeviceIndex or PreferredDeviceUrl is configured."));
                return false;
            }

            // Connect to selected device
            if (!runtime.ConnectDevice(selectedUrl))
            {
                ErrorOccurred?.Invoke(this, ET5PluginException.ConnectionFailed(
                    runtime.GetLastErrorDescription()));
                return false;
            }

            // Subscribe gaze
            if (!runtime.SubscribeGaze(nativeGazeCallback))
            {
                ErrorOccurred?.Invoke(this, ET5PluginException.CallbackFailure(
                    "Failed to subscribe to gaze point stream."));
                runtime.DisconnectDevice();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Resolves the device URL to use based on developer configuration.
        /// Returns null if no valid selection can be made.
        ///
        /// NEVER auto-selects deviceUrls[0] without explicit configuration.
        /// Device URLs are not logged to protect privacy.
        /// </summary>
        private string ResolveDeviceUrl(List<string> deviceUrls)
        {
            // Explicit URL takes precedence
            if (!string.IsNullOrEmpty(configuration.PreferredDeviceUrl))
            {
                // Validate the URL exists in the enumerated list
                if (deviceUrls.Contains(configuration.PreferredDeviceUrl))
                {
                    logger.Info("Developer mode: using explicitly configured device URL.");
                    return configuration.PreferredDeviceUrl;
                }
                else
                {
                    logger.Warn("Developer mode: configured device URL was not found in enumeration.");
                    // Fall through to index-based selection
                }
            }

            // Index-based selection
            if (configuration.PreferredDeviceIndex.HasValue)
            {
                int idx = configuration.PreferredDeviceIndex.Value;
                if (idx >= 0 && idx < deviceUrls.Count)
                {
                    logger.Info($"Developer mode: selecting device at index {idx} of {deviceUrls.Count}.");
                    return deviceUrls[idx];
                }
                else
                {
                    logger.Warn($"Developer mode: PreferredDeviceIndex {idx} is out of range " +
                        $"(enumerated {deviceUrls.Count} device(s)).");
                    return null;
                }
            }

            // No selection configured
            return null;
        }

        private void HandleStreamError(tobii_error_t error)
        {
            logger.Warn($"Tobii stream error: {error}. Transitioning to Reconnecting.");
            stateMachine.TryTransition(GazeServiceState.Reconnecting,
                new GazeServiceError(error.ToString(), runtime.GetLastErrorDescription()));

            ConnectionStatusChanged?.Invoke(this, false);
            ErrorOccurred?.Invoke(this, ET5PluginException.ConnectionLost(error.ToString()));

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

                bool workerClean = (workerThread == null || !workerThread.IsAlive);
                if (workerClean)
                {
                    runtime.Dispose();
                }
                else
                {
                    logger.Warn("Worker thread is still active after Stop() timeout; skipping runtime.Dispose() to prevent native use-after-free.");
                }

                logger.Info("TobiiGazeProvider disposed.");
            }
        }
    }
}

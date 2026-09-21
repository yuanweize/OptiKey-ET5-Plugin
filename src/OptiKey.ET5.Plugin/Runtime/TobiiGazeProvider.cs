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
    /// Implements:
    ///   - Single-device automatic binding for ordinary end-users.
    ///   - Multi-device safety: strictly refuses silent auto-binding when multiple devices exist.
    ///   - Integrated ICallbackPump lifecycle (Polling or WaitAndProcess).
    ///   - Bounded shutdown lifecycle with stuck-worker isolation to prevent host crashes.
    ///   - Generation-tracked worker threads to eliminate Start/Stop/Dispose races.
    /// </summary>
    public class TobiiGazeProvider : IGazeProvider
    {
        private readonly ITobiiRuntime runtime;
        private readonly IReconnectPolicy reconnectPolicy;
        private readonly IPluginLogger logger;
        private readonly GazeServiceStateMachine stateMachine;
        private readonly Func<ITobiiRuntime, Action<tobii_error_t>, ICallbackPump> callbackPumpFactory;

        private PluginConfiguration configuration;
        private Thread workerThread;
        private CancellationTokenSource cts;
        private int lifecycleGeneration;
        private ICallbackPump callbackPump;
        private readonly object lifecycleLock = new object();
        private readonly tobii_gaze_point_callback_t nativeGazeCallback; // Prevent GC collection of delegate

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
            TimeSpan? stopTimeout = null,
            Func<ITobiiRuntime, Action<tobii_error_t>, ICallbackPump> callbackPumpFactory = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiGazeProvider));
            this.runtime = runtime ?? new TobiiNativeRuntime(logger: this.logger);
            this.reconnectPolicy = reconnectPolicy ?? new ExponentialBackoffReconnectPolicy();
            this.stateMachine = stateMachine ?? new GazeServiceStateMachine();
            this.callbackPumpFactory = callbackPumpFactory;

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

                int generation = ++lifecycleGeneration;
                cts = new CancellationTokenSource();
                var token = cts.Token;
                stateMachine.TryTransition(GazeServiceState.Starting);

                workerThread = new Thread(WorkerLoop)
                {
                    Name = "TobiiGazeProviderWorker",
                    IsBackground = true
                };
                workerThread.Start(new WorkerParams { Token = token, Generation = generation });
                logger.Info("Tobii gaze provider worker thread started.");
            }
        }

        public void Stop()
        {
            Thread threadToJoin = null;
            CancellationTokenSource ctsToCancel = null;
            ICallbackPump pumpToStop = null;

            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed || stateMachine.CurrentState == GazeServiceState.Stopped)
                {
                    return;
                }

                logger.Info("Stopping Tobii gaze provider...");
                stateMachine.TryTransition(GazeServiceState.Stopping);

                ctsToCancel = cts;
                cts = null;

                pumpToStop = callbackPump;
                callbackPump = null;

                threadToJoin = workerThread;
                workerThread = null;
            }

            if (ctsToCancel != null)
            {
                try { ctsToCancel.Cancel(); } catch (ObjectDisposedException) { }
            }

            if (pumpToStop != null)
            {
                try
                {
                    pumpToStop.RequestStop();
                    pumpToStop.Join(TimeSpan.FromMilliseconds(Math.Min(stopTimeout.TotalMilliseconds, 200)));
                }
                catch (Exception ex)
                {
                    logger.Warn($"Exception while stopping callback pump: {ex.Message}");
                }
                finally
                {
                    try { pumpToStop.Dispose(); } catch { }
                }
            }

            bool joined = true;
            if (threadToJoin != null && threadToJoin.IsAlive && threadToJoin != Thread.CurrentThread)
            {
                joined = threadToJoin.Join(stopTimeout);
            }

            lock (lifecycleLock)
            {
                if (!joined)
                {
                    logger.Error(
                        $"CRITICAL: Worker thread did not terminate within {stopTimeout.TotalSeconds:F1}s. " +
                        "Native thread is potentially stuck in tobii_wait_for_callbacks. " +
                        "Aborting native handle cleanup to prevent use-after-free.");

                    stateMachine.TryTransition(GazeServiceState.Stopped,
                        new GazeServiceError("STUCK_WORKER", "Worker thread failed to terminate during Stop()."));
                    ConnectionStatusChanged?.Invoke(this, false);
                    return;
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

        private void WorkerLoop(object state)
        {
            var p = (WorkerParams)state;
            var token = p.Token;
            int generation = p.Generation;
            int reconnectAttempt = 0;

            try
            {
                while (!token.IsCancellationRequested && generation == this.lifecycleGeneration)
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

                                StartCallbackPump();

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

                                if (WaitOrCancel(delayMs, token))
                                {
                                    break;
                                }
                                continue;
                            }
                        }

                        if (stateMachine.CurrentState == GazeServiceState.Connected)
                        {
                            lock (lifecycleLock)
                            {
                                if (callbackPump != null && callbackPump.State == CallbackPumpState.Faulted)
                                {
                                    StopCallbackPump();
                                    stateMachine.TryTransition(GazeServiceState.Reconnecting,
                                        new GazeServiceError("PUMP_FAULT", "Callback pump entered faulted state."));
                                    ConnectionStatusChanged?.Invoke(this, false);
                                    continue;
                                }
                            }

                            if (WaitOrCancel(100, token))
                            {
                                break;
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (token.IsCancellationRequested || generation != this.lifecycleGeneration)
                        {
                            break;
                        }

                        logger.Error("Unexpected exception in Tobii worker loop.", ex);
                        ErrorOccurred?.Invoke(this, ex);

                        stateMachine.TryTransition(GazeServiceState.Reconnecting,
                            new GazeServiceError("EXCEPTION", ex.Message, ex));

                        if (WaitOrCancel(1000, token))
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception fatalEx)
            {
                logger.Error("Fatal unhandled exception in Tobii worker thread.", fatalEx);
                ErrorOccurred?.Invoke(this, fatalEx);
            }
            finally
            {
                StopCallbackPump();
                logger.Debug("Worker loop exited.");
            }
        }

        private void StartCallbackPump()
        {
            lock (lifecycleLock)
            {
                if (callbackPump != null)
                {
                    return;
                }

                if (callbackPumpFactory != null)
                {
                    callbackPump = callbackPumpFactory(runtime, HandleStreamError);
                }
                else
                {
                    callbackPump = CreateDefaultCallbackPump(runtime, configuration, HandleStreamError, logger);
                }

                callbackPump.PumpError += (sender, ex) =>
                {
                    logger.Warn($"Callback pump error event received: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                };

                callbackPump.Start();
            }
        }

        private void StopCallbackPump()
        {
            ICallbackPump pumpToStop = null;
            lock (lifecycleLock)
            {
                pumpToStop = callbackPump;
                callbackPump = null;
            }

            if (pumpToStop != null)
            {
                try
                {
                    pumpToStop.RequestStop();
                    pumpToStop.Join(stopTimeout);
                }
                catch (Exception ex)
                {
                    logger.Warn($"Exception while stopping callback pump: {ex.Message}");
                }
                finally
                {
                    try { pumpToStop.Dispose(); } catch { }
                }
            }
        }

        private static ICallbackPump CreateDefaultCallbackPump(
            ITobiiRuntime runtime,
            PluginConfiguration config,
            Action<tobii_error_t> streamErrorCallback,
            IPluginLogger logger)
        {
            if (config?.CallbackStrategy == CallbackStrategy.WaitAndProcess)
            {
                return new WaitAndProcessCallbackPump(runtime, streamErrorCallback, logger);
            }

            // Default: ProcessOnlyPollingPump (guarantees interruptible, bounded shutdown)
            return new ProcessOnlyPollingPump(runtime, streamErrorCallback, config?.PollIntervalMs ?? 5, logger);
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

            string selectedUrl = ResolveDeviceCandidate(deviceUrls);
            if (selectedUrl == null)
            {
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
        /// Resolves the device URL to use based on scientific device selection policy (Phase 6):
        /// 1. Explicit PreferredDeviceUrl or PreferredDeviceIndex overrides.
        /// 2. If exactly ONE device candidate exists and AutomaticDeviceSelection is enabled (default), binds automatically.
        /// 3. If MULTIPLE device candidates exist, strictly refuses silent auto-binding of candidate 0.
        /// 4. If automatic selection is disabled, requires explicit user configuration.
        /// </summary>
        private string ResolveDeviceCandidate(List<string> deviceUrls)
        {
            // Priority 1: Explicit URL or Index configured by user or developer
            if (!string.IsNullOrEmpty(configuration.PreferredDeviceUrl))
            {
                if (deviceUrls.Contains(configuration.PreferredDeviceUrl))
                {
                    logger.Info("Using explicitly configured device URL.");
                    return configuration.PreferredDeviceUrl;
                }
                logger.Error("Configured PreferredDeviceUrl was not found in enumerated devices.");
                ErrorOccurred?.Invoke(this, new ET5PluginException(
                    ET5ErrorCode.DeviceNotFound,
                    "Specified device URL was not found.",
                    $"Enumerated {deviceUrls.Count} devices."));
                return null;
            }

            if (configuration.PreferredDeviceIndex.HasValue)
            {
                int idx = configuration.PreferredDeviceIndex.Value;
                if (idx >= 0 && idx < deviceUrls.Count)
                {
                    logger.Info($"Using explicitly configured device index: {idx}");
                    return deviceUrls[idx];
                }

                logger.Error($"Configured PreferredDeviceIndex {idx} is out of range (count: {deviceUrls.Count}).");
                ErrorOccurred?.Invoke(this, new ET5PluginException(
                    ET5ErrorCode.DeviceNotFound,
                    $"Device index {idx} out of range.",
                    $"Enumerated {deviceUrls.Count} devices."));
                return null;
            }

            // Priority 2: Exactly ONE candidate exists and AutomaticDeviceSelection (or AllowUnverifiedTobiiDevice) is enabled
            if (deviceUrls.Count == 1 && (configuration.AutomaticDeviceSelection || configuration.AllowUnverifiedTobiiDevice))
            {
                logger.Info("Single Tobii device candidate detected and plugin selected by user; automatically binding.");
                return deviceUrls[0];
            }

            // Priority 3: Multiple candidates exist -> NEVER silently auto-select candidate 0!
            if (deviceUrls.Count > 1)
            {
                logger.Warn($"Multiple Tobii device candidates detected ({deviceUrls.Count}). " +
                    "Automatic selection refused to prevent connecting to an unintended device. " +
                    "Please configure PreferredDeviceIndex in %APPDATA%\\OptiKey-ET5-Plugin\\et5-plugin.config.");
                ErrorOccurred?.Invoke(this, new ET5PluginException(
                    ET5ErrorCode.DeviceIdentityUnknown,
                    "Multiple Tobii devices detected.",
                    $"Enumerated {deviceUrls.Count} devices. Specify PreferredDeviceIndex (0, 1, ...) in config."));
                return null;
            }

            // Priority 4: Single candidate but automatic selection and developer override are disabled
            logger.Warn("Automatic device selection is disabled and no explicit device was configured; refusing connection.");
            ErrorOccurred?.Invoke(this, ET5PluginException.DeviceIdentityUnknown());
            return null;
        }

        private void HandleStreamError(tobii_error_t error)
        {
            logger.Warn($"Tobii stream error: {error}. Transitioning to Reconnecting.");
            stateMachine.TryTransition(GazeServiceState.Reconnecting,
                new GazeServiceError(error.ToString(), runtime.GetLastErrorDescription()));

            ConnectionStatusChanged?.Invoke(this, false);
            ErrorOccurred?.Invoke(this, ET5PluginException.ConnectionLost(error.ToString()));

            StopCallbackPump();

            // Attempt reconnect through native runtime first
            if (!runtime.ReconnectDevice())
            {
                runtime.UnsubscribeGaze();
                runtime.DisconnectDevice();
            }
        }

        private void OnNativeGazePoint(ref tobii_gaze_point_t gazePoint, IntPtr userData)
        {
            if (stateMachine.CurrentState != GazeServiceState.Connected)
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
            try
            {
                return token.WaitHandle.WaitOne(milliseconds);
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        public void Dispose()
        {
            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed)
                {
                    return;
                }
            }

            Stop();

            lock (lifecycleLock)
            {
                if (stateMachine.CurrentState == GazeServiceState.Disposed)
                {
                    return;
                }

                stateMachine.ForceDisposed();

                var oldCts = cts;
                cts = null;
                try { oldCts?.Dispose(); } catch { }

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

        private class WorkerParams
        {
            public CancellationToken Token { get; set; }
            public int Generation { get; set; }
        }
    }
}

using System;
using System.Reactive;
using System.Windows;
using JuliusSweetland.OptiKey.Contracts;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Diagnostics;
using OptiKey.ET5.Plugin.Mapping;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.State;

namespace OptiKey.ET5.Plugin
{
    /// <summary>
    /// Production OptiKey eye-tracker plugin for Tobii Eye Tracker 5 (ADR-001, ADR-003).
    /// Implements JuliusSweetland.OptiKey.Contracts.IPointService.
    /// </summary>
    public class ET5PointService : IPointService, IDisposable
    {
        private readonly IGazeProvider gazeProvider;
        private readonly ScreenCoordinateMapper coordinateMapper;
        private readonly IPluginLogger logger;
        private readonly object eventLock = new object();

        private EventHandler<Timestamped<Point>> pointEvent;
        private EventHandler<Exception> errorEvent;
        private bool isDisposed = false;

        #region Constructors

        /// <summary>
        /// Public parameterless constructor required by OptiKey's reflection loader (ADR-003).
        /// GUARANTEE: Zero side effects. Does not access hardware, driver services, or native DLLs.
        /// Activator.CreateInstance() will NEVER fail or throw from this constructor.
        /// </summary>
        public ET5PointService()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// Internal constructor enabling dependency injection for testing and mock providers.
        /// </summary>
        internal ET5PointService(
            IGazeProvider gazeProvider = null,
            ScreenCoordinateMapper coordinateMapper = null,
            IPluginLogger logger = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(ET5PointService));
            this.coordinateMapper = coordinateMapper ?? new ScreenCoordinateMapper();
            this.gazeProvider = gazeProvider ?? new TobiiGazeProvider(logger: this.logger);

            // Wire provider events
            this.gazeProvider.GazePointAvailable += OnGazePointAvailable;
            this.gazeProvider.ErrorOccurred += OnProviderErrorOccurred;

            this.logger.Info("ET5PointService constructed successfully (hardware-independent).");
        }

        #endregion

        #region IPointService / INotifyErrors Events

        /// <summary>
        /// Emitted on every mapped physical screen gaze point (ADR-007).
        /// Adding the first listener automatically starts the background gaze stream.
        /// Removing the last listener stops the stream.
        /// </summary>
        public event EventHandler<Timestamped<Point>> Point
        {
            add
            {
                lock (eventLock)
                {
                    pointEvent += value;
                    EnsureStarted();
                }
            }
            remove
            {
                lock (eventLock)
                {
                    pointEvent -= value;
                    if (pointEvent == null)
                    {
                        EnsureStopped();
                    }
                }
            }
        }

        /// <summary>
        /// INotifyErrors event emitted when device connection or hardware errors occur.
        /// </summary>
        public event EventHandler<Exception> Error
        {
            add
            {
                lock (eventLock)
                {
                    errorEvent += value;
                }
            }
            remove
            {
                lock (eventLock)
                {
                    errorEvent -= value;
                }
            }
        }

        #endregion

        #region Event Handlers & Coordinate Mapping

        private void OnGazePointAvailable(object sender, GazePointEventArgs e)
        {
            if (isDisposed) return;

            // Map normalized sensor coordinates [0.0, 1.0] to physical screen pixels (ADR-007)
            if (coordinateMapper.TryMap(e, out Point screenPixelPoint))
            {
                var timestamped = new Timestamped<Point>(screenPixelPoint, e.UtcTimestamp);

                EventHandler<Timestamped<Point>> handler;
                lock (eventLock)
                {
                    handler = pointEvent;
                }

                handler?.Invoke(this, timestamped);
            }
        }

        private void OnProviderErrorOccurred(object sender, Exception ex)
        {
            if (isDisposed) return;

            logger.Error("Eye tracker provider reported an error.", ex);

            EventHandler<Exception> handler;
            lock (eventLock)
            {
                handler = errorEvent;
            }

            handler?.Invoke(this, ex);
        }

        #endregion

        #region Lifecycle Controls

        private void EnsureStarted()
        {
            if (isDisposed) return;

            try
            {
                logger.Info("Initiating eye tracker gaze streaming...");
                gazeProvider.Start();
            }
            catch (Exception ex)
            {
                logger.Error("Failed to start gaze provider.", ex);
                OnProviderErrorOccurred(this, ex);
            }
        }

        private void EnsureStopped()
        {
            if (isDisposed) return;

            try
            {
                logger.Info("Stopping eye tracker gaze streaming...");
                gazeProvider.Stop();
            }
            catch (Exception ex)
            {
                logger.Warn($"Exception while stopping gaze provider: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            lock (eventLock)
            {
                if (isDisposed) return;
                isDisposed = true;

                logger.Info("Disposing ET5PointService...");

                // Unhook events
                gazeProvider.GazePointAvailable -= OnGazePointAvailable;
                gazeProvider.ErrorOccurred -= OnProviderErrorOccurred;

                try
                {
                    gazeProvider.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Warn($"Exception disposing gaze provider: {ex.Message}");
                }

                pointEvent = null;
                errorEvent = null;

                logger.Info("ET5PointService disposed cleanly.");
            }
        }

        #endregion
    }
}

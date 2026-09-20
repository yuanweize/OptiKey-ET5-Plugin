using System;
using System.Collections.Generic;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Default implementation of ITobiiRuntime binding to physical Tobii Stream Engine DLL.
    /// </summary>
    public class TobiiNativeRuntime : ITobiiRuntime
    {
        private readonly TobiiStreamEngineBinding binding;
        private readonly ITobiiRuntimeLocator locator;
        private readonly IPluginLogger logger;

        private IntPtr apiContext = IntPtr.Zero;
        private IntPtr deviceContext = IntPtr.Zero;
        private readonly object syncLock = new object();
        private tobii_error_t lastError = tobii_error_t.TOBII_ERROR_NO_ERROR;

        public TobiiNativeRuntime(
            ITobiiRuntimeLocator locator = null,
            IPluginLogger logger = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiNativeRuntime));
            this.locator = locator ?? new TobiiRuntimeLocator(this.logger);
            this.binding = new TobiiStreamEngineBinding(this.logger);
        }

        public bool Initialize()
        {
            lock (syncLock)
            {
                if (apiContext != IntPtr.Zero)
                {
                    return true;
                }

                var locateResult = locator.LocateRuntime();
                if (!locateResult.IsFound)
                {
                    logger.Warn($"Cannot initialize Tobii runtime: {locateResult.FailureReason}");
                    return false;
                }

                if (!binding.Load(locateResult.LibraryPath))
                {
                    return false;
                }

                lastError = binding.ApiCreate(out apiContext);
                if (lastError != tobii_error_t.TOBII_ERROR_NO_ERROR)
                {
                    logger.Error($"tobii_api_create failed: {binding.GetErrorMessage(lastError)}");
                    return false;
                }

                return true;
            }
        }

        public bool EnumerateDevices(out List<string> urls)
        {
            urls = new List<string>();
            lock (syncLock)
            {
                if (apiContext == IntPtr.Zero && !Initialize())
                {
                    return false;
                }

                lastError = binding.EnumerateDeviceUrls(apiContext, out urls);
                if (lastError != tobii_error_t.TOBII_ERROR_NO_ERROR)
                {
                    logger.Warn($"Failed to enumerate Tobii devices: {binding.GetErrorMessage(lastError)}");
                    return false;
                }

                return true;
            }
        }

        public bool ConnectDevice(string url)
        {
            lock (syncLock)
            {
                if (apiContext == IntPtr.Zero && !Initialize())
                {
                    return false;
                }

                DisconnectDevice();

                logger.Info($"Connecting to Tobii device at URL: {url}");
                lastError = binding.DeviceCreate(apiContext, url, out deviceContext);
                if (lastError != tobii_error_t.TOBII_ERROR_NO_ERROR)
                {
                    logger.Error($"Failed to create Tobii device: {binding.GetErrorMessage(lastError)}");
                    deviceContext = IntPtr.Zero;
                    return false;
                }

                return true;
            }
        }

        public bool DisconnectDevice()
        {
            lock (syncLock)
            {
                if (deviceContext != IntPtr.Zero)
                {
                    binding.DeviceDestroy(deviceContext);
                    deviceContext = IntPtr.Zero;
                }
                return true;
            }
        }

        public bool ReconnectDevice()
        {
            lock (syncLock)
            {
                if (deviceContext == IntPtr.Zero)
                {
                    return false;
                }

                lastError = binding.DeviceReconnect(deviceContext);
                return lastError == tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
        }

        public bool SubscribeGaze(tobii_gaze_point_callback_t callback)
        {
            lock (syncLock)
            {
                if (deviceContext == IntPtr.Zero)
                {
                    return false;
                }

                lastError = binding.GazePointSubscribe(deviceContext, callback);
                if (lastError != tobii_error_t.TOBII_ERROR_NO_ERROR)
                {
                    logger.Error($"Failed to subscribe to gaze points: {binding.GetErrorMessage(lastError)}");
                    return false;
                }

                return true;
            }
        }

        public bool UnsubscribeGaze()
        {
            lock (syncLock)
            {
                if (deviceContext != IntPtr.Zero)
                {
                    binding.GazePointUnsubscribe(deviceContext);
                }
                return true;
            }
        }

        public tobii_error_t WaitForCallbacks()
        {
            IntPtr dev;
            lock (syncLock)
            {
                dev = deviceContext;
            }

            if (dev == IntPtr.Zero)
            {
                return tobii_error_t.TOBII_ERROR_NOT_AVAILABLE;
            }

            return binding.WaitForCallbacks(new[] { dev });
        }

        public tobii_error_t ProcessCallbacks()
        {
            IntPtr dev;
            lock (syncLock)
            {
                dev = deviceContext;
            }

            if (dev == IntPtr.Zero)
            {
                return tobii_error_t.TOBII_ERROR_NOT_AVAILABLE;
            }

            return binding.ProcessCallbacks(dev);
        }

        public bool TryGetDeviceInfo(out tobii_device_info_t info)
        {
            info = default(tobii_device_info_t);
            return false;
        }

        public string GetLastErrorDescription()
        {
            return binding.GetErrorMessage(lastError);
        }

        public void Dispose()
        {
            lock (syncLock)
            {
                DisconnectDevice();

                if (apiContext != IntPtr.Zero)
                {
                    binding.ApiDestroy(apiContext);
                    apiContext = IntPtr.Zero;
                }

                binding.Dispose();
            }
        }
    }
}

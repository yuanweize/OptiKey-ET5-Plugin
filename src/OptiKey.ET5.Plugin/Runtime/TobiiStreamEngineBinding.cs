using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OptiKey.ET5.Plugin.Diagnostics;

namespace OptiKey.ET5.Plugin.Runtime
{
    #region Enums and Structs (ADR-004 & docs/RUNTIME_RESEARCH.md)

    public enum tobii_error_t
    {
        TOBII_ERROR_NO_ERROR = 0,
        TOBII_ERROR_INTERNAL = 1,
        TOBII_ERROR_INSUFFICIENT_LICENSE = 2,
        TOBII_ERROR_NOT_SUPPORTED = 3,
        TOBII_ERROR_NOT_AVAILABLE = 4,
        TOBII_ERROR_CONNECTION_FAILED = 5,
        TOBII_ERROR_TIMED_OUT = 6,
        TOBII_ERROR_ALLOCATION_FAILED = 7,
        TOBII_ERROR_INVALID_PARAMETER = 8,
        TOBII_ERROR_CALIBRATION_ALREADY_STARTED = 9,
        TOBII_ERROR_CALIBRATION_NOT_STARTED = 10,
        TOBII_ERROR_ALREADY_SUBSCRIBED = 11,
        TOBII_ERROR_NOT_SUBSCRIBED = 12,
        TOBII_ERROR_OPERATION_FAILED = 13,
        TOBII_ERROR_CONFLICTING_API_INSTANCES = 14,
        TOBII_ERROR_CALIBRATION_BUSY = 15,
        TOBII_ERROR_CALLBACK_IN_PROGRESS = 16,
        TOBII_ERROR_TOO_MANY_SUBSCRIBERS = 17,
        TOBII_ERROR_CONNECTION_FAILED_DRIVER = 18,
        TOBII_ERROR_UNAUTHORIZED = 19
    }

    public enum tobii_validity_t
    {
        TOBII_VALIDITY_INVALID = 0,
        TOBII_VALIDITY_VALID = 1
    }

    public enum tobii_field_of_use_t
    {
        TOBII_FIELD_OF_USE_INTERACTIVE = 1,
        TOBII_FIELD_OF_USE_ANALYTICAL = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct tobii_gaze_point_t
    {
        public long timestamp_us;
        public tobii_validity_t validity;
        public float position_x;
        public float position_y;
    }

    #endregion

    #region Delegates

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void tobii_device_url_receiver_t(string url, IntPtr user_data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void tobii_gaze_point_callback_t(ref tobii_gaze_point_t gaze_point, IntPtr user_data);

    #endregion

    /// <summary>
    /// Dynamic binding layer for tobii_stream_engine.dll using a verified absolute path.
    /// </summary>
    public class TobiiStreamEngineBinding : IDisposable
    {
        private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
        private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW")]
        private static extern IntPtr LoadLibraryEx(string fileName, IntPtr fileHandle, uint flags);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        private IntPtr moduleHandle = IntPtr.Zero;
        private readonly IPluginLogger logger;

        // Native function delegates
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_api_create_delegate(out IntPtr api, IntPtr custom_alloc, IntPtr custom_log);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_api_destroy_delegate(IntPtr api);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_enumerate_local_device_urls_delegate(IntPtr api, tobii_device_url_receiver_t receiver, IntPtr user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_device_create_delegate(IntPtr api, string url, tobii_field_of_use_t field_of_use, out IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_device_destroy_delegate(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_device_reconnect_delegate(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_wait_for_callbacks_delegate(IntPtr device_count, IntPtr[] devices);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_device_process_callbacks_delegate(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_gaze_point_subscribe_delegate(IntPtr device, tobii_gaze_point_callback_t callback, IntPtr user_data);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate tobii_error_t tobii_gaze_point_unsubscribe_delegate(IntPtr device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr tobii_error_message_delegate(tobii_error_t error);

        private tobii_api_create_delegate fn_tobii_api_create;
        private tobii_api_destroy_delegate fn_tobii_api_destroy;
        private tobii_enumerate_local_device_urls_delegate fn_tobii_enumerate_local_device_urls;
        private tobii_device_create_delegate fn_tobii_device_create;
        private tobii_device_destroy_delegate fn_tobii_device_destroy;
        private tobii_device_reconnect_delegate fn_tobii_device_reconnect;
        private tobii_wait_for_callbacks_delegate fn_tobii_wait_for_callbacks;
        private tobii_device_process_callbacks_delegate fn_tobii_device_process_callbacks;
        private tobii_gaze_point_subscribe_delegate fn_tobii_gaze_point_subscribe;
        private tobii_gaze_point_unsubscribe_delegate fn_tobii_gaze_point_unsubscribe;
        private tobii_error_message_delegate fn_tobii_error_message;

        public bool IsLoaded => moduleHandle != IntPtr.Zero;

        public TobiiStreamEngineBinding(IPluginLogger logger = null)
        {
            this.logger = logger ?? new PluginLogger(typeof(TobiiStreamEngineBinding));
        }

        public bool Load(string libraryPath)
        {
            if (IsLoaded)
            {
                return true;
            }

            logger.Info($"Loading native Tobii library from: {libraryPath}");
            if (!Path.IsPathRooted(libraryPath))
            {
                throw new ArgumentException("The Tobii runtime path must be absolute.", nameof(libraryPath));
            }

            moduleHandle = LoadLibraryEx(libraryPath, IntPtr.Zero,
                LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
            if (moduleHandle == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                logger.Error($"Failed to LoadLibrary for {libraryPath}. Win32Error: {err}");
                return false;
            }

            try
            {
                fn_tobii_api_create = Bind<tobii_api_create_delegate>("tobii_api_create");
                fn_tobii_api_destroy = Bind<tobii_api_destroy_delegate>("tobii_api_destroy");
                fn_tobii_enumerate_local_device_urls = Bind<tobii_enumerate_local_device_urls_delegate>("tobii_enumerate_local_device_urls");
                fn_tobii_device_create = Bind<tobii_device_create_delegate>("tobii_device_create");
                fn_tobii_device_destroy = Bind<tobii_device_destroy_delegate>("tobii_device_destroy");
                fn_tobii_device_reconnect = Bind<tobii_device_reconnect_delegate>("tobii_device_reconnect");
                fn_tobii_wait_for_callbacks = Bind<tobii_wait_for_callbacks_delegate>("tobii_wait_for_callbacks");
                fn_tobii_device_process_callbacks = Bind<tobii_device_process_callbacks_delegate>("tobii_device_process_callbacks");
                fn_tobii_gaze_point_subscribe = Bind<tobii_gaze_point_subscribe_delegate>("tobii_gaze_point_subscribe");
                fn_tobii_gaze_point_unsubscribe = Bind<tobii_gaze_point_unsubscribe_delegate>("tobii_gaze_point_unsubscribe");
                fn_tobii_error_message = Bind<tobii_error_message_delegate>("tobii_error_message");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to bind essential Tobii Stream Engine functions.", ex);
                Dispose();
                return false;
            }
        }

        private T Bind<T>(string procName) where T : class
        {
            IntPtr proc = GetProcAddress(moduleHandle, procName);
            if (proc == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException($"Tobii Stream Engine entry point '{procName}' was not found.");
            }
            return Marshal.GetDelegateForFunctionPointer(proc, typeof(T)) as T;
        }

        public tobii_error_t ApiCreate(out IntPtr api)
        {
            EnsureLoaded();
            return fn_tobii_api_create(out api, IntPtr.Zero, IntPtr.Zero);
        }

        public tobii_error_t ApiDestroy(IntPtr api)
        {
            if (!IsLoaded || api == IntPtr.Zero) return tobii_error_t.TOBII_ERROR_NO_ERROR;
            return fn_tobii_api_destroy(api);
        }

        public tobii_error_t EnumerateDeviceUrls(IntPtr api, out List<string> urls)
        {
            EnsureLoaded();
            var list = new List<string>();
            tobii_device_url_receiver_t receiver = (url, data) =>
            {
                if (!string.IsNullOrEmpty(url)) list.Add(url);
            };

            var err = fn_tobii_enumerate_local_device_urls(api, receiver, IntPtr.Zero);
            urls = list;
            return err;
        }

        public tobii_error_t DeviceCreate(IntPtr api, string url, out IntPtr device)
        {
            EnsureLoaded();
            // Strictly enforce TOBII_FIELD_OF_USE_INTERACTIVE (ADR-004)
            return fn_tobii_device_create(api, url, tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out device);
        }

        public tobii_error_t DeviceDestroy(IntPtr device)
        {
            if (!IsLoaded || device == IntPtr.Zero) return tobii_error_t.TOBII_ERROR_NO_ERROR;
            return fn_tobii_device_destroy(device);
        }

        public tobii_error_t DeviceReconnect(IntPtr device)
        {
            EnsureLoaded();
            return fn_tobii_device_reconnect(device);
        }

        public tobii_error_t WaitForCallbacks(IntPtr[] devices)
        {
            EnsureLoaded();
            return fn_tobii_wait_for_callbacks((IntPtr)devices.Length, devices);
        }

        public tobii_error_t ProcessCallbacks(IntPtr device)
        {
            EnsureLoaded();
            return fn_tobii_device_process_callbacks(device);
        }

        public tobii_error_t GazePointSubscribe(IntPtr device, tobii_gaze_point_callback_t callback)
        {
            EnsureLoaded();
            return fn_tobii_gaze_point_subscribe(device, callback, IntPtr.Zero);
        }

        public tobii_error_t GazePointUnsubscribe(IntPtr device)
        {
            EnsureLoaded();
            return fn_tobii_gaze_point_unsubscribe(device);
        }

        public string GetErrorMessage(tobii_error_t error)
        {
            if (!IsLoaded || fn_tobii_error_message == null) return error.ToString();
            try
            {
                IntPtr ptr = fn_tobii_error_message(error);
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : error.ToString();
            }
            catch
            {
                return error.ToString();
            }
        }

        private void EnsureLoaded()
        {
            if (!IsLoaded)
            {
                throw new InvalidOperationException("Tobii Stream Engine native library is not loaded.");
            }
        }

        public void Dispose()
        {
            if (moduleHandle != IntPtr.Zero)
            {
                FreeLibrary(moduleHandle);
                moduleHandle = IntPtr.Zero;
            }
        }
    }
}

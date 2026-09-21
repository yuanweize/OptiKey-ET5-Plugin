using System;
using System.Collections.Generic;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Describes the objective capabilities discovered from a loaded Tobii Stream Engine
    /// runtime, without prescribing which callback strategy to use.
    ///
    /// Capability flags are set exclusively by probing exports via GetProcAddress.
    /// A symbol being present does NOT upgrade ABI confidence; it only records availability.
    /// </summary>
    public sealed class RuntimeCapabilities
    {
        // --- Core Required: must be present for any runtime interaction ---
        public bool HasApiCreate { get; }
        public bool HasApiDestroy { get; }
        public bool HasEnumerateDevices { get; }

        // --- Device Required: must be present for device lifecycle ---
        public bool HasDeviceCreate { get; }
        public bool HasDeviceDestroy { get; }

        // --- Gaze Required: must be present for gaze data subscription ---
        public bool HasGazePointSubscribe { get; }
        public bool HasGazePointUnsubscribe { get; }
        public bool HasProcessCallbacks { get; }

        // --- Strategy Dependent: availability depends on callback strategy selection ---
        public bool HasWaitForCallbacks { get; }

        // --- Optional: graceful degradation if missing ---
        public bool HasDeviceReconnect { get; }
        public bool HasErrorMessage { get; }

        /// <summary>
        /// Runtime version string extracted from file metadata, if available.
        /// This is informational only and does not affect binding.
        /// </summary>
        public string RuntimeVersion { get; }

        /// <summary>
        /// Absolute path of the loaded runtime binary.
        /// </summary>
        public string RuntimePath { get; }

        /// <summary>
        /// Symbols classified as required that were not found.
        /// </summary>
        public IReadOnlyList<string> MissingRequired { get; }

        /// <summary>
        /// Symbols classified as optional/strategy-dependent that were not found.
        /// </summary>
        public IReadOnlyList<string> MissingOptional { get; }

        // --- Composite capability queries (NOT strategy decisions) ---

        /// <summary>
        /// True if the runtime can create and destroy an API context and enumerate devices.
        /// This is the minimum for diagnostics/inventory without device interaction.
        /// </summary>
        public bool CanCreateApi => HasApiCreate && HasApiDestroy && HasEnumerateDevices;

        /// <summary>
        /// True if the runtime can enumerate and then create/destroy a device.
        /// </summary>
        public bool CanEnumerate => CanCreateApi;

        /// <summary>
        /// True if the runtime can create and destroy devices.
        /// </summary>
        public bool CanCreateDevice => CanCreateApi && HasDeviceCreate && HasDeviceDestroy;

        /// <summary>
        /// True if the runtime can subscribe and unsubscribe gaze callbacks.
        /// Does NOT determine whether WaitForCallbacks or polling is required.
        /// </summary>
        public bool CanSubscribeGaze => CanCreateDevice
            && HasGazePointSubscribe
            && HasGazePointUnsubscribe
            && HasProcessCallbacks;

        public RuntimeCapabilities(
            bool hasApiCreate,
            bool hasApiDestroy,
            bool hasEnumerateDevices,
            bool hasDeviceCreate,
            bool hasDeviceDestroy,
            bool hasGazePointSubscribe,
            bool hasGazePointUnsubscribe,
            bool hasProcessCallbacks,
            bool hasWaitForCallbacks,
            bool hasDeviceReconnect,
            bool hasErrorMessage,
            IReadOnlyList<string> missingRequired,
            IReadOnlyList<string> missingOptional,
            string runtimeVersion = null,
            string runtimePath = null)
        {
            HasApiCreate = hasApiCreate;
            HasApiDestroy = hasApiDestroy;
            HasEnumerateDevices = hasEnumerateDevices;
            HasDeviceCreate = hasDeviceCreate;
            HasDeviceDestroy = hasDeviceDestroy;
            HasGazePointSubscribe = hasGazePointSubscribe;
            HasGazePointUnsubscribe = hasGazePointUnsubscribe;
            HasProcessCallbacks = hasProcessCallbacks;
            HasWaitForCallbacks = hasWaitForCallbacks;
            HasDeviceReconnect = hasDeviceReconnect;
            HasErrorMessage = hasErrorMessage;
            MissingRequired = missingRequired ?? Array.AsReadOnly(new string[0]);
            MissingOptional = missingOptional ?? Array.AsReadOnly(new string[0]);
            RuntimeVersion = runtimeVersion;
            RuntimePath = runtimePath;
        }
    }

    /// <summary>
    /// Enumerates the possible callback processing strategies.
    /// The actual strategy is selected at runtime based on capabilities and future
    /// research evidence, NOT hardcoded into the capabilities model.
    /// </summary>
    public enum CallbackProcessingStrategy
    {
        /// <summary>
        /// Strategy not yet determined. No gaze processing should proceed.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Use tobii_wait_for_callbacks then tobii_device_process_callbacks.
        /// Requires both exports to be present. Blocking semantics and shutdown
        /// behavior are not yet proven for the supported runtime.
        /// </summary>
        WaitAndProcess = 1,

        /// <summary>
        /// Use only tobii_device_process_callbacks in a polling loop.
        /// Requires process_callbacks export. Does not depend on wait_for_callbacks.
        /// </summary>
        ProcessOnly = 2,

        /// <summary>
        /// All native interaction is isolated in a separate RuntimeHost process.
        /// The plugin communicates via local IPC (named pipe).
        /// </summary>
        RuntimeHostIsolated = 3
    }
}

using System;

namespace OptiKey.ET5.Plugin.Core
{
    /// <summary>
    /// Stable error codes for all ET5 plugin failure categories.
    /// These codes are intended for programmatic matching and user-facing diagnostics.
    /// </summary>
    public enum ET5ErrorCode
    {
        /// <summary>No error.</summary>
        None = 0,

        /// <summary>Tobii runtime DLL was not found on the system.</summary>
        RuntimeNotFound,

        /// <summary>Tobii runtime was found but rejected (architecture, signer, trust).</summary>
        RuntimeRejected,

        /// <summary>Runtime is loaded but missing required exports for the requested operation.</summary>
        RuntimeIncompatible,

        /// <summary>A specific required export symbol was not found in the runtime.</summary>
        RequiredExportMissing,

        /// <summary>No Tobii devices were found during enumeration.</summary>
        DeviceNotFound,

        /// <summary>Device was found but identity could not be verified as ET5.</summary>
        DeviceIdentityUnknown,

        /// <summary>Device connection attempt failed.</summary>
        ConnectionFailed,

        /// <summary>Previously connected device became unreachable.</summary>
        ConnectionLost,

        /// <summary>Native callback processing encountered an error.</summary>
        CallbackFailure,

        /// <summary>Display metrics are unavailable or invalid for coordinate mapping.</summary>
        DisplayUnavailable,

        /// <summary>Tobii native API returned an error not mapped to a specific code.</summary>
        NativeApiError,

        /// <summary>Plugin configuration is invalid.</summary>
        ConfigurationError,

        /// <summary>An unexpected internal error occurred.</summary>
        InternalError
    }

    /// <summary>
    /// Unified exception type for all ET5 plugin errors. Contains separate user-facing
    /// and diagnostic messages so that OptiKey can display helpful information to
    /// accessibility users while developers see full technical detail.
    ///
    /// All errors propagate: runtime → provider → ET5PointService.Error → OptiKey.
    /// </summary>
    public class ET5PluginException : Exception
    {
        /// <summary>
        /// Stable error code for programmatic matching.
        /// </summary>
        public ET5ErrorCode Code { get; }

        /// <summary>
        /// Short, localization-ready message suitable for displaying to the end user.
        /// Must not contain technical implementation details, device URLs, or file paths.
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// Detailed technical message for developer diagnostics and logs.
        /// May contain paths, error codes, and stack information.
        /// Must not contain gaze coordinates, user identity, or serial numbers.
        /// </summary>
        public string DiagnosticMessage { get; }

        public ET5PluginException(
            ET5ErrorCode code,
            string userMessage,
            string diagnosticMessage = null,
            Exception innerException = null)
            : base(diagnosticMessage ?? userMessage, innerException)
        {
            Code = code;
            UserMessage = userMessage ?? string.Empty;
            DiagnosticMessage = diagnosticMessage ?? userMessage ?? string.Empty;
        }

        public override string ToString()
        {
            return $"[ET5-{Code}] {DiagnosticMessage}"
                + (InnerException != null ? $"\n  Inner: {InnerException.Message}" : string.Empty);
        }

        // --- Factory methods for common error scenarios ---

        public static ET5PluginException RuntimeNotFound(string diagnosticDetail = null)
        {
            return new ET5PluginException(
                ET5ErrorCode.RuntimeNotFound,
                "Tobii Eye Tracker software was not found. Please install Tobii Experience.",
                diagnosticDetail ?? "Tobii Stream Engine runtime was not located on this system.");
        }

        public static ET5PluginException RuntimeRejected(string reason)
        {
            return new ET5PluginException(
                ET5ErrorCode.RuntimeRejected,
                "The Tobii runtime could not be verified. Please reinstall Tobii Experience.",
                $"Runtime rejected: {reason}");
        }

        public static ET5PluginException RuntimeIncompatible(string detail)
        {
            return new ET5PluginException(
                ET5ErrorCode.RuntimeIncompatible,
                "The installed Tobii software version is not compatible with this plugin.",
                $"Runtime incompatible: {detail}");
        }

        public static ET5PluginException RequiredExportMissing(string symbolName)
        {
            return new ET5PluginException(
                ET5ErrorCode.RequiredExportMissing,
                "The installed Tobii software is missing required functionality.",
                $"Required export '{symbolName}' was not found in the loaded runtime.");
        }

        public static ET5PluginException DeviceNotFound()
        {
            return new ET5PluginException(
                ET5ErrorCode.DeviceNotFound,
                "No Tobii eye tracker was detected. Please check the USB connection.",
                "Device enumeration returned zero candidates.");
        }

        public static ET5PluginException DeviceIdentityUnknown()
        {
            return new ET5PluginException(
                ET5ErrorCode.DeviceIdentityUnknown,
                "A Tobii device was found but could not be identified as Eye Tracker 5.",
                "Device identity verification is not yet available. Use developer test mode with explicit device selection for controlled testing.");
        }

        public static ET5PluginException ConnectionFailed(string nativeError = null)
        {
            return new ET5PluginException(
                ET5ErrorCode.ConnectionFailed,
                "Could not connect to the eye tracker. Please check that Tobii Experience is running.",
                $"Device connection failed: {nativeError ?? "unknown error"}");
        }

        public static ET5PluginException ConnectionLost(string nativeError = null)
        {
            return new ET5PluginException(
                ET5ErrorCode.ConnectionLost,
                "Connection to the eye tracker was lost. Attempting to reconnect...",
                $"Device connection lost: {nativeError ?? "unknown error"}");
        }

        public static ET5PluginException CallbackFailure(string detail, Exception inner = null)
        {
            return new ET5PluginException(
                ET5ErrorCode.CallbackFailure,
                "Eye tracker data processing encountered an error.",
                $"Callback failure: {detail}",
                inner);
        }

        public static ET5PluginException DisplayUnavailable()
        {
            return new ET5PluginException(
                ET5ErrorCode.DisplayUnavailable,
                "Display information is not available for coordinate mapping.",
                "DisplayMetrics could not be obtained for the primary display.");
        }

        public static ET5PluginException NativeApiError(string functionName, string nativeError)
        {
            return new ET5PluginException(
                ET5ErrorCode.NativeApiError,
                "The eye tracker reported an internal error.",
                $"Native API error in {functionName}: {nativeError}");
        }
    }
}

using System;
using System.Collections.Generic;

namespace OptiKey.ET5.Plugin.Runtime
{
    /// <summary>
    /// Abstract runtime contract for Tobii Stream Engine operations.
    /// Allows mocking for unit tests without needing physical hardware.
    /// </summary>
    public interface ITobiiRuntime : IDisposable
    {
        bool Initialize();
        bool EnumerateDevices(out List<string> urls);
        bool ConnectDevice(string url);
        bool DisconnectDevice();
        bool ReconnectDevice();
        bool SubscribeGaze(tobii_gaze_point_callback_t callback);
        bool UnsubscribeGaze();
        tobii_error_t WaitForCallbacks();
        tobii_error_t ProcessCallbacks();
        bool TryGetDeviceInfo(out tobii_device_info_t info);
        string GetLastErrorDescription();
    }
}

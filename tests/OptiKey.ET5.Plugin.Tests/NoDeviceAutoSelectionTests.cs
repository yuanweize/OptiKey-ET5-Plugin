using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Diagnostics;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.State;

namespace OptiKey.ET5.Plugin.Tests
{
    /// <summary>
    /// Explicit regression tests verifying that the plugin NEVER automatically
    /// connects to the first enumerated device (deviceUrls[0]) in any mode.
    /// Explicit user selection (index or URL) is strictly required.
    /// </summary>
    [TestFixture]
    public class NoDeviceAutoSelectionTests
    {
        [Test]
        public void DeveloperMode_WithMultipleDevices_NoSelection_NeverAutoSelectsFirstDevice()
        {
            var fakeRuntime = new TrackingFakeRuntime(new[]
            {
                "tobii-prx://device-candidate-0",
                "tobii-prx://device-candidate-1"
            });

            // Developer mode is enabled, but no PreferredDeviceIndex or PreferredDeviceUrl is set
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = null,
                PreferredDeviceUrl = null
            };

            var errors = new List<Exception>();
            var provider = new TobiiGazeProvider(fakeRuntime, configuration: config);
            provider.ErrorOccurred += (s, e) => errors.Add(e);

            provider.Start();

            // Wait for connection attempt to complete
            Thread.Sleep(100);
            provider.Stop();

            // Verification:
            // 1. Device was NEVER connected
            Assert.That(fakeRuntime.ConnectedUrl, Is.Null, "Candidate 0 must NOT be automatically selected");
            Assert.That(fakeRuntime.DeviceWasConnected, Is.False);

            // 2. ErrorOccurred received ET5PluginException with DeviceIdentityUnknown
            Assert.That(errors, Has.Some.TypeOf<ET5PluginException>());
            var identityEx = errors.Find(e => e is ET5PluginException pe && pe.Code == ET5ErrorCode.DeviceIdentityUnknown) as ET5PluginException;
            Assert.That(identityEx, Is.Not.Null);
            Assert.That(identityEx.Code, Is.EqualTo(ET5ErrorCode.DeviceIdentityUnknown));
        }

        [Test]
        public void DeveloperMode_WithSingleDevice_NoSelection_NeverAutoSelectsSoleDevice()
        {
            var fakeRuntime = new TrackingFakeRuntime(new[]
            {
                "tobii-prx://sole-candidate-device"
            });

            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = null,
                PreferredDeviceUrl = null
            };

            var errors = new List<Exception>();
            var provider = new TobiiGazeProvider(fakeRuntime, configuration: config);
            provider.ErrorOccurred += (s, e) => errors.Add(e);

            provider.Start();
            Thread.Sleep(100);
            provider.Stop();

            Assert.That(fakeRuntime.ConnectedUrl, Is.Null, "Even a single candidate must NOT be automatically selected without explicit config");
            Assert.That(fakeRuntime.DeviceWasConnected, Is.False);
            Assert.That(errors, Has.Some.TypeOf<ET5PluginException>());
            var identityEx = errors.Find(e => e is ET5PluginException pe && pe.Code == ET5ErrorCode.DeviceIdentityUnknown) as ET5PluginException;
            Assert.That(identityEx, Is.Not.Null);
        }

        [Test]
        public void DeveloperMode_WithExplicitIndex_SelectsExactlyThatDevice()
        {
            var fakeRuntime = new TrackingFakeRuntime(new[]
            {
                "tobii-prx://device-candidate-0",
                "tobii-prx://device-candidate-1"
            });

            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 1
            };

            var provider = new TobiiGazeProvider(fakeRuntime, configuration: config);
            provider.Start();

            Thread.Sleep(100);
            provider.Stop();

            Assert.That(fakeRuntime.ConnectedUrl, Is.EqualTo("tobii-prx://device-candidate-1"),
                "Explicit index 1 must select the second device");
            Assert.That(fakeRuntime.DeviceWasConnected, Is.True);
        }

        private class TrackingFakeRuntime : ITobiiRuntime
        {
            private readonly string[] urls;
            public bool DeviceWasConnected { get; private set; }
            public string ConnectedUrl { get; private set; }

            public TrackingFakeRuntime(string[] urls)
            {
                this.urls = urls ?? new string[0];
            }

            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> deviceUrls)
            {
                deviceUrls = new List<string>(urls);
                return true;
            }
            public bool ConnectDevice(string url)
            {
                DeviceWasConnected = true;
                ConnectedUrl = url;
                return true;
            }
            public bool DisconnectDevice()
            {
                DeviceWasConnected = false;
                ConnectedUrl = null;
                return true;
            }
            public bool ReconnectDevice() => false;
            public bool SubscribeGaze(tobii_gaze_point_callback_t callback) => true;
            public bool UnsubscribeGaze() => true;
            public tobii_error_t WaitForCallbacks()
            {
                Thread.Sleep(20);
                return tobii_error_t.TOBII_ERROR_TIMED_OUT;
            }
            public tobii_error_t ProcessCallbacks() => tobii_error_t.TOBII_ERROR_NO_ERROR;
            public string GetLastErrorDescription() => "tracking fake";
            public void Dispose() { }
        }
    }
}

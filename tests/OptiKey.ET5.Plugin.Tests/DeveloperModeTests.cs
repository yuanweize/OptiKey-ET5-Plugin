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
    [TestFixture]
    public class DeveloperModeTests
    {
        #region Strict mode (default) always refuses

        [Test]
        public void StrictMode_Default_RefusesConnection()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A", "tobii://device-B" });
            var config = new PluginConfiguration(); // default: strict mode
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            Exception lastError = null;
            provider.ErrorOccurred += (s, e) => lastError = e;

            provider.Start();
            // Let worker attempt once
            Thread.Sleep(500);
            provider.Stop();

            // Should refuse in strict mode even with devices present
            Assert.IsFalse(fakeRuntime.DeviceWasConnected, "Strict mode must not connect to any device.");
            Assert.IsNotNull(lastError);
            Assert.IsInstanceOf<ET5PluginException>(lastError);
            Assert.AreEqual(ET5ErrorCode.DeviceIdentityUnknown, ((ET5PluginException)lastError).Code);

            provider.Dispose();
        }

        #endregion

        #region Developer mode: no auto-selection without explicit index/URL

        [Test]
        public void DeveloperMode_NoIndex_NoUrl_RefusesConnection()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true
                // PreferredDeviceIndex is null
                // PreferredDeviceUrl is null
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            Exception lastError = null;
            provider.ErrorOccurred += (s, e) => lastError = e;

            provider.Start();
            Thread.Sleep(500);
            provider.Stop();

            Assert.IsFalse(fakeRuntime.DeviceWasConnected,
                "Developer mode without explicit selection must NOT auto-connect first device.");
            Assert.IsNotNull(lastError);
            Assert.IsInstanceOf<ET5PluginException>(lastError);
            Assert.AreEqual(ET5ErrorCode.DeviceIdentityUnknown, ((ET5PluginException)lastError).Code);

            provider.Dispose();
        }

        #endregion

        #region Developer mode: explicit index selects correct device

        [Test]
        public void DeveloperMode_WithIndex0_ConnectsFirstDevice()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A", "tobii://device-B" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 0
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            provider.Start();
            Thread.Sleep(500);

            Assert.IsTrue(fakeRuntime.DeviceWasConnected, "Should connect with explicit index 0.");
            Assert.AreEqual("tobii://device-A", fakeRuntime.ConnectedUrl);

            provider.Stop();
            provider.Dispose();
        }

        [Test]
        public void DeveloperMode_WithIndex1_ConnectsSecondDevice()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A", "tobii://device-B" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 1
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            provider.Start();
            Thread.Sleep(500);

            Assert.IsTrue(fakeRuntime.DeviceWasConnected);
            Assert.AreEqual("tobii://device-B", fakeRuntime.ConnectedUrl);

            provider.Stop();
            provider.Dispose();
        }

        #endregion

        #region Developer mode: out-of-range index fails

        [Test]
        public void DeveloperMode_OutOfRangeIndex_RefusesConnection()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 5 // only 1 device
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            Exception lastError = null;
            provider.ErrorOccurred += (s, e) => lastError = e;

            provider.Start();
            Thread.Sleep(500);
            provider.Stop();

            Assert.IsFalse(fakeRuntime.DeviceWasConnected);

            provider.Dispose();
        }

        #endregion

        #region Developer mode: explicit URL

        [Test]
        public void DeveloperMode_WithMatchingUrl_Connects()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A", "tobii://device-B" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceUrl = "tobii://device-B"
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            provider.Start();
            Thread.Sleep(500);

            Assert.IsTrue(fakeRuntime.DeviceWasConnected);
            Assert.AreEqual("tobii://device-B", fakeRuntime.ConnectedUrl);

            provider.Stop();
            provider.Dispose();
        }

        [Test]
        public void DeveloperMode_WithNonMatchingUrl_NoIndex_Fails()
        {
            var fakeRuntime = new FakeRuntimeWithDevices(new[] { "tobii://device-A" });
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceUrl = "tobii://nonexistent"
                // No PreferredDeviceIndex fallback
            };
            var sm = new GazeServiceStateMachine();
            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stateMachine: sm);

            provider.Start();
            Thread.Sleep(500);
            provider.Stop();

            Assert.IsFalse(fakeRuntime.DeviceWasConnected,
                "Non-matching URL with no index fallback should fail.");

            provider.Dispose();
        }

        #endregion

        #region Configuration loading

        [Test]
        public void PluginConfiguration_DefaultIsStrictMode()
        {
            var config = new PluginConfiguration();
            Assert.IsFalse(config.AllowUnverifiedTobiiDevice);
            Assert.IsNull(config.PreferredDeviceIndex);
            Assert.IsNull(config.PreferredDeviceUrl);
        }

        #endregion

        #region Helper: fake runtime with device URLs

        private class FakeRuntimeWithDevices : ITobiiRuntime
        {
            private readonly string[] deviceUrls;
            public bool DeviceWasConnected { get; private set; }
            public string ConnectedUrl { get; private set; }

            public FakeRuntimeWithDevices(string[] urls)
            {
                deviceUrls = urls ?? new string[0];
            }

            public bool Initialize() => true;

            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string>(deviceUrls);
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
                // Simulate blocking for a short time then return
                Thread.Sleep(50);
                return tobii_error_t.TOBII_ERROR_TIMED_OUT;
            }

            public tobii_error_t ProcessCallbacks() => tobii_error_t.TOBII_ERROR_NO_ERROR;

            public string GetLastErrorDescription() => "fake runtime";

            public void Dispose() { }
        }

        #endregion
    }
}

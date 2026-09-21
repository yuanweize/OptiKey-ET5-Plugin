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
    /// Tests handling of mid-stream runtime disconnects, reconnection attempts,
    /// and cancellation during reconnection.
    /// </summary>
    [TestFixture]
    public class RuntimeDisconnectTests
    {
        [Test]
        public void StreamFailure_TransitionsToReconnecting_AndNotifiesDisconnect()
        {
            var disconnectRuntime = new DisconnectableFakeRuntime();
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 0
            };

            var statusNotifications = new List<bool>();
            var provider = new TobiiGazeProvider(
                runtime: disconnectRuntime,
                configuration: config,
                stopTimeout: TimeSpan.FromSeconds(1));

            provider.ConnectionStatusChanged += (s, isConnected) =>
            {
                lock (statusNotifications)
                {
                    statusNotifications.Add(isConnected);
                }
            };

            provider.Start();

            // Wait for initial connection
            for (int i = 0; i < 50 && !provider.IsConnected; i++)
            {
                Thread.Sleep(10);
            }
            Assert.That(provider.IsConnected, Is.True, "Should connect initially");

            // Inject mid-stream disconnect and prevent immediate reconnection
            disconnectRuntime.AllowReconnect = false;
            disconnectRuntime.TriggerDisconnect();

            // Wait for transition to Reconnecting
            for (int i = 0; i < 50 && provider.StateMachine.CurrentState != GazeServiceState.Reconnecting; i++)
            {
                Thread.Sleep(20);
            }

            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Reconnecting));

            lock (statusNotifications)
            {
                Assert.That(statusNotifications, Contains.Item(false),
                    "ConnectionStatusChanged(false) must be raised on disconnect");
            }

            provider.Stop();
            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Stopped));
            provider.Dispose();
        }

        [Test]
        public void ReconnectFailure_CanBeStoppedCleanlyDuringBackoff()
        {
            var disconnectRuntime = new DisconnectableFakeRuntime();
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 0
            };

            var provider = new TobiiGazeProvider(
                runtime: disconnectRuntime,
                configuration: config,
                stopTimeout: TimeSpan.FromSeconds(1));

            provider.Start();

            for (int i = 0; i < 50 && !provider.IsConnected; i++)
            {
                Thread.Sleep(10);
            }

            // Trigger disconnect and prevent reconnection
            disconnectRuntime.TriggerDisconnect();
            disconnectRuntime.AllowReconnect = false;

            for (int i = 0; i < 50 && provider.StateMachine.CurrentState != GazeServiceState.Reconnecting; i++)
            {
                Thread.Sleep(20);
            }

            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Reconnecting));

            // Stop during backoff wait should return promptly
            var sw = System.Diagnostics.Stopwatch.StartNew();
            provider.Stop();
            sw.Stop();

            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1500), "Stop() during backoff must not block indefinitely");
            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Stopped));

            provider.Dispose();
        }

        private class DisconnectableFakeRuntime : ITobiiRuntime
        {
            private volatile bool isDisconnected = false;
            public bool AllowReconnect { get; set; } = true;

            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string> { "tobii-prx://disconnect-device" };
                return true;
            }

            public bool ConnectDevice(string url)
            {
                return !isDisconnected || AllowReconnect;
            }

            public bool DisconnectDevice() => true;
            public bool ReconnectDevice() => AllowReconnect;
            public bool SubscribeGaze(tobii_gaze_point_callback_t callback) => true;
            public bool UnsubscribeGaze() => true;

            public void TriggerDisconnect()
            {
                isDisconnected = true;
            }

            public tobii_error_t WaitForCallbacks()
            {
                if (isDisconnected)
                {
                    return tobii_error_t.TOBII_ERROR_CONNECTION_FAILED;
                }
                Thread.Sleep(10);
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }

            public tobii_error_t ProcessCallbacks()
            {
                if (isDisconnected)
                {
                    return tobii_error_t.TOBII_ERROR_CONNECTION_FAILED;
                }
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }

            public string GetLastErrorDescription() => isDisconnected ? "Disconnected" : "OK";
            public void Dispose() { }
        }
    }
}

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
    /// Verifies that repeated Start/Stop and connect/disconnect cycles do not leak
    /// threads, corrupt state, or cause deadlocks.
    /// </summary>
    [TestFixture]
    public class RepeatedLifecycleTests
    {
        [Test]
        public void Provider_RepeatedStartStopCycles_MaintainConsistentState()
        {
            var fakeRuntime = new LifecycleFakeRuntime();
            var config = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 0
            };

            var provider = new TobiiGazeProvider(
                runtime: fakeRuntime,
                configuration: config,
                stopTimeout: TimeSpan.FromSeconds(1));

            for (int cycle = 0; cycle < 5; cycle++)
            {
                provider.Start();

                // Wait until connected
                for (int i = 0; i < 50 && !provider.IsConnected; i++)
                {
                    Thread.Sleep(10);
                }
                Assert.That(provider.IsConnected, Is.True, $"Cycle {cycle}: should reach Connected state");

                provider.Stop();
                Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Stopped),
                    $"Cycle {cycle}: should reach Stopped state");
            }

            provider.Dispose();
            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Disposed));
        }

        [Test]
        public void WaitAndProcessPump_RepeatedStartStopCycles_ExecuteCleanly()
        {
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var pump = new WaitAndProcessCallbackPump(
                    waitFunc: () =>
                    {
                        Thread.Sleep(5);
                        return tobii_error_t.TOBII_ERROR_NO_ERROR;
                    },
                    processFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR);

                pump.Start();
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Running));

                Thread.Sleep(20);
                pump.RequestStop();
                bool stopped = pump.Join(TimeSpan.FromSeconds(1));

                Assert.That(stopped, Is.True, $"Cycle {cycle}: pump should join cleanly");
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Stopped));

                pump.Dispose();
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Disposed));
            }
        }

        [Test]
        public void PollingPump_RepeatedStartStopCycles_ExecuteCleanly()
        {
            for (int cycle = 0; cycle < 5; cycle++)
            {
                var pump = new ProcessOnlyPollingPump(
                    processFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR,
                    pollIntervalMs: 2);

                pump.Start();
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Running));

                Thread.Sleep(15);
                pump.RequestStop();
                bool stopped = pump.Join(TimeSpan.FromSeconds(1));

                Assert.That(stopped, Is.True, $"Cycle {cycle}: polling pump should join cleanly");
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Stopped));

                pump.Dispose();
                Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Disposed));
            }
        }

        private class LifecycleFakeRuntime : ITobiiRuntime
        {
            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string> { "tobii-prx://lifecycle-device" };
                return true;
            }
            public bool ConnectDevice(string url) => true;
            public bool DisconnectDevice() => true;
            public bool ReconnectDevice() => false;
            public bool SubscribeGaze(tobii_gaze_point_callback_t callback) => true;
            public bool UnsubscribeGaze() => true;
            public tobii_error_t WaitForCallbacks()
            {
                Thread.Sleep(5);
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
            public tobii_error_t ProcessCallbacks() => tobii_error_t.TOBII_ERROR_NO_ERROR;
            public string GetLastErrorDescription() => "lifecycle fake";
            public void Dispose() { }
        }
    }
}

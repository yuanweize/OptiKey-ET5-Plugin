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
    public class CallbackShutdownTests
    {
        #region WaitAndProcessCallbackPump Tests

        [Test]
        public void WaitAndProcessPump_ImmediateReturn_StopsCleanly()
        {
            int cycleCount = 0;
            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () =>
                {
                    Thread.Sleep(5);
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                },
                processFunc: () =>
                {
                    Interlocked.Increment(ref cycleCount);
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                });

            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Idle));

            pump.Start();
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Running));

            Thread.Sleep(50);
            Assert.That(cycleCount, Is.GreaterThan(0));

            pump.RequestStop();
            bool stopped = pump.Join(TimeSpan.FromSeconds(1));

            Assert.That(stopped, Is.True);
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Stopped));

            pump.Dispose();
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Disposed));
        }

        [Test]
        public void WaitAndProcessPump_StuckWorker_TimesOutGracefully_EntersTimedOutState()
        {
            var enteredWaitEvent = new ManualResetEventSlim(false);
            var unblockEvent = new ManualResetEventSlim(false);

            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () =>
                {
                    // Signal that worker has reliably entered the blocking native wait
                    enteredWaitEvent.Set();
                    unblockEvent.Wait();
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                },
                processFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR);

            pump.Start();
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Running));

            // Ensure worker thread is actively blocked inside waitFunc before requesting stop
            Assert.That(enteredWaitEvent.Wait(TimeSpan.FromSeconds(3)), Is.True, "Worker must enter waitFunc before stop request");

            // Request stop while worker is stuck in waitFunc
            pump.RequestStop();

            // Join with very short timeout
            bool stopped = pump.Join(TimeSpan.FromMilliseconds(50));

            Assert.That(stopped, Is.False, "Join must time out when worker is blocked");
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.TimedOut));

            // Clean up: unblock worker so thread exits
            unblockEvent.Set();
            Thread.Sleep(50);
            pump.Dispose();
        }

        [Test]
        public void WaitAndProcessPump_WaitException_TransitionsToFaultedAndFiresPumpError()
        {
            Exception caughtEx = null;
            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () => throw new InvalidOperationException("Native wait crash"),
                processFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR);

            pump.PumpError += (s, e) => caughtEx = e;
            pump.Start();

            pump.Join(TimeSpan.FromSeconds(1));

            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Faulted));
            Assert.That(caughtEx, Is.Not.Null);
            Assert.That(caughtEx.Message, Does.Contain("Native wait crash"));
        }

        [Test]
        public void WaitAndProcessPump_ProcessException_TransitionsToFaultedAndFiresPumpError()
        {
            Exception caughtEx = null;
            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR,
                processFunc: () => throw new AccessViolationException("Native process memory violation"));

            pump.PumpError += (s, e) => caughtEx = e;
            pump.Start();

            pump.Join(TimeSpan.FromSeconds(1));

            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Faulted));
            Assert.That(caughtEx, Is.Not.Null);
            Assert.That(caughtEx.Message, Does.Contain("Native process memory violation"));
        }

        [Test]
        public void WaitAndProcessPump_StreamError_InvokesCallback()
        {
            tobii_error_t? reportedError = null;
            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR,
                processFunc: () => tobii_error_t.TOBII_ERROR_CONNECTION_FAILED,
                streamErrorCallback: err => reportedError = err);

            pump.Start();
            Thread.Sleep(50);
            pump.RequestStop();
            pump.Join(TimeSpan.FromSeconds(1));

            Assert.That(reportedError, Is.EqualTo(tobii_error_t.TOBII_ERROR_CONNECTION_FAILED));
        }

        [Test]
        public void WaitAndProcessPump_IdempotentStartAndStop()
        {
            var pump = new WaitAndProcessCallbackPump(
                waitFunc: () =>
                {
                    Thread.Sleep(5);
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                },
                processFunc: () => tobii_error_t.TOBII_ERROR_NO_ERROR);

            pump.Start();
            pump.Start(); // Duplicate start is no-op

            pump.RequestStop();
            pump.RequestStop(); // Duplicate request stop is safe

            bool stopped1 = pump.Join(TimeSpan.FromSeconds(1));
            bool stopped2 = pump.Join(TimeSpan.FromSeconds(1)); // Repeated join returns immediately

            Assert.That(stopped1, Is.True);
            Assert.That(stopped2, Is.True);
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Stopped));

            pump.Dispose();
            pump.Dispose(); // Repeated dispose is safe
        }

        #endregion

        #region ProcessOnlyPollingPump Tests

        [Test]
        public void PollingPump_NormalPolling_StopsPromptly()
        {
            int cycleCount = 0;
            var pump = new ProcessOnlyPollingPump(
                processFunc: () =>
                {
                    Interlocked.Increment(ref cycleCount);
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                },
                pollIntervalMs: 5);

            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Idle));

            pump.Start();
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Running));

            Thread.Sleep(50);
            Assert.That(cycleCount, Is.GreaterThan(0));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            pump.RequestStop();
            bool stopped = pump.Join(TimeSpan.FromSeconds(1));
            sw.Stop();

            Assert.That(stopped, Is.True);
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(200), "Polling pump should wake and exit promptly");
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Stopped));

            pump.Dispose();
            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Disposed));
        }

        [Test]
        public void PollingPump_Exception_TransitionsToFaulted()
        {
            Exception caughtEx = null;
            var pump = new ProcessOnlyPollingPump(
                processFunc: () => throw new InvalidOperationException("Poll crash"),
                pollIntervalMs: 5);

            pump.PumpError += (s, e) => caughtEx = e;
            pump.Start();

            pump.Join(TimeSpan.FromSeconds(1));

            Assert.That(pump.State, Is.EqualTo(CallbackPumpState.Faulted));
            Assert.That(caughtEx, Is.Not.Null);
            Assert.That(caughtEx.Message, Does.Contain("Poll crash"));
        }

        [Test]
        public void PollingPump_IgnoresTimedOutError_ReportsFatalErrors()
        {
            var reportedErrors = new List<tobii_error_t>();
            int callIndex = 0;

            var pump = new ProcessOnlyPollingPump(
                processFunc: () =>
                {
                    callIndex++;
                    if (callIndex == 1) return tobii_error_t.TOBII_ERROR_TIMED_OUT; // Normal poll timeout
                    if (callIndex == 2) return tobii_error_t.TOBII_ERROR_CONNECTION_FAILED; // Real error
                    return tobii_error_t.TOBII_ERROR_NO_ERROR;
                },
                streamErrorCallback: err => reportedErrors.Add(err),
                pollIntervalMs: 5);

            pump.Start();
            Thread.Sleep(60);
            pump.RequestStop();
            pump.Join(TimeSpan.FromSeconds(1));

            // TOBII_ERROR_TIMED_OUT must be filtered out for polling
            Assert.That(reportedErrors, Does.Not.Contain(tobii_error_t.TOBII_ERROR_TIMED_OUT));
            Assert.That(reportedErrors, Contains.Item(tobii_error_t.TOBII_ERROR_CONNECTION_FAILED));
        }

        #endregion

        #region Provider Stuck Worker Lifecycle Tests (ADR requirement)

        [Test]
        public void Provider_WhenWorkerStuck_StopTimesOut_SkipsNativeTeardownToPreventUseAfterFree()
        {
            var unblockEvent = new ManualResetEventSlim(false);
            var stuckRuntime = new StuckFakeRuntime(unblockEvent);

            var devConfig = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                CallbackStrategy = CallbackStrategy.WaitAndProcess,
                PreferredDeviceIndex = 0
            };

            // Provider configured with a 50ms stop timeout for fast, deterministic testing
            var provider = new TobiiGazeProvider(
                runtime: stuckRuntime,
                configuration: devConfig,
                stopTimeout: TimeSpan.FromMilliseconds(50));

            provider.Start();

            // Wait until connected and streaming
            for (int i = 0; i < 50 && !provider.IsConnected; i++)
            {
                Thread.Sleep(20);
            }
            Assert.That(provider.IsConnected, Is.True, "Provider should connect to fake device");
            Assert.That(stuckRuntime.EnteredWaitEvent.Wait(TimeSpan.FromSeconds(3)), Is.True, "Worker must enter wait before stop");

            // Stop provider while native wait is permanently blocked
            var sw = System.Diagnostics.Stopwatch.StartNew();
            provider.Stop();
            sw.Stop();

            // Verification 1: Stop() did not hang indefinitely
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(1500), "Stop() must return within bounded timeout");

            // Verification 2: State transitioned to Stopped with STUCK_WORKER error
            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Stopped));
            Assert.That(provider.StateMachine.LastError, Is.Not.Null);
            Assert.That(provider.StateMachine.LastError.ErrorCode, Is.EqualTo("STUCK_WORKER"));

            // Verification 3: Native teardown was skipped to prevent use-after-free
            Assert.That(stuckRuntime.UnsubscribeGazeCalled, Is.False,
                "UnsubscribeGaze MUST NOT be called when worker is stuck");
            Assert.That(stuckRuntime.DisconnectDeviceCalled, Is.False,
                "DisconnectDevice MUST NOT be called when worker is stuck");

            // Verification 4: Dispose() also respects active stuck worker and skips runtime.Dispose()
            provider.Dispose();
            Assert.That(stuckRuntime.DisposeCalled, Is.False,
                "runtime.Dispose MUST NOT be called when worker is still alive");

            // Clean up test thread
            unblockEvent.Set();
            Thread.Sleep(50);
        }

        [Test]
        public void Provider_WhenWorkerHealthy_StopSucceedsAndPerformsCleanTeardown()
        {
            var healthyRuntime = new HealthyFakeRuntime();

            var devConfig = new PluginConfiguration
            {
                AllowUnverifiedTobiiDevice = true,
                PreferredDeviceIndex = 0
            };

            var provider = new TobiiGazeProvider(
                runtime: healthyRuntime,
                configuration: devConfig,
                stopTimeout: TimeSpan.FromSeconds(1));

            provider.Start();

            for (int i = 0; i < 50 && !provider.IsConnected; i++)
            {
                Thread.Sleep(20);
            }
            Assert.That(provider.IsConnected, Is.True);

            provider.Stop();

            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Stopped));
            Assert.That(healthyRuntime.UnsubscribeGazeCalled, Is.True, "Clean stop must unsubscribe gaze");
            Assert.That(healthyRuntime.DisconnectDeviceCalled, Is.True, "Clean stop must disconnect device");

            provider.Dispose();
            Assert.That(healthyRuntime.DisposeCalled, Is.True, "Clean disposal must dispose runtime");
        }

        [Test]
        public void Provider_WhenDisposedWhileWorkerRunning_DoesNotThrowNullReferenceException()
        {
            var unblockEvent = new ManualResetEventSlim(false);
            var stuckRuntime = new StuckFakeRuntime(unblockEvent);
            var devConfig = new PluginConfiguration
            {
                CallbackStrategy = CallbackStrategy.WaitAndProcess,
                PreferredDeviceIndex = 0
            };

            var provider = new TobiiGazeProvider(
                runtime: stuckRuntime,
                configuration: devConfig,
                stopTimeout: TimeSpan.FromMilliseconds(50));

            provider.Start();

            // Wait until connected
            for (int i = 0; i < 50 && !provider.IsConnected; i++)
            {
                Thread.Sleep(10);
            }
            Assert.That(stuckRuntime.EnteredWaitEvent.Wait(TimeSpan.FromSeconds(3)), Is.True, "Worker must enter wait before dispose");

            // Dispose while worker is blocked in WaitForCallbacks
            provider.Dispose();

            // Unblock worker thread - previously this caused NullReferenceException at WorkerLoop line 221!
            unblockEvent.Set();
            Thread.Sleep(50);

            Assert.That(provider.StateMachine.CurrentState, Is.EqualTo(GazeServiceState.Disposed));
        }

        #endregion

        #region Helper Fake Runtimes

        private class StuckFakeRuntime : ITobiiRuntime
        {
            private readonly ManualResetEventSlim unblockEvent;
            public ManualResetEventSlim EnteredWaitEvent { get; } = new ManualResetEventSlim(false);
            public bool UnsubscribeGazeCalled { get; private set; }
            public bool DisconnectDeviceCalled { get; private set; }
            public bool DisposeCalled { get; private set; }

            public StuckFakeRuntime(ManualResetEventSlim unblockEvent)
            {
                this.unblockEvent = unblockEvent;
            }

            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string> { "tobii-prx://fake-device" };
                return true;
            }
            public bool ConnectDevice(string url) => true;
            public bool DisconnectDevice()
            {
                DisconnectDeviceCalled = true;
                return true;
            }
            public bool ReconnectDevice() => false;
            public bool SubscribeGaze(tobii_gaze_point_callback_t callback) => true;
            public bool UnsubscribeGaze()
            {
                UnsubscribeGazeCalled = true;
                return true;
            }
            public tobii_error_t WaitForCallbacks()
            {
                EnteredWaitEvent.Set();
                unblockEvent.Wait();
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
            public tobii_error_t ProcessCallbacks() => tobii_error_t.TOBII_ERROR_NO_ERROR;
            public string GetLastErrorDescription() => "stuck fake";
            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        private class HealthyFakeRuntime : ITobiiRuntime
        {
            public bool UnsubscribeGazeCalled { get; private set; }
            public bool DisconnectDeviceCalled { get; private set; }
            public bool DisposeCalled { get; private set; }

            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string> { "tobii-prx://fake-device" };
                return true;
            }
            public bool ConnectDevice(string url) => true;
            public bool DisconnectDevice()
            {
                DisconnectDeviceCalled = true;
                return true;
            }
            public bool ReconnectDevice() => false;
            public bool SubscribeGaze(tobii_gaze_point_callback_t callback) => true;
            public bool UnsubscribeGaze()
            {
                UnsubscribeGazeCalled = true;
                return true;
            }
            public tobii_error_t WaitForCallbacks()
            {
                Thread.Sleep(10);
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
            public tobii_error_t ProcessCallbacks() => tobii_error_t.TOBII_ERROR_NO_ERROR;
            public string GetLastErrorDescription() => "healthy fake";
            public void Dispose()
            {
                DisposeCalled = true;
            }
        }

        #endregion
    }
}

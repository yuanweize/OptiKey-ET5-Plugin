using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Mapping;
using OptiKey.ET5.Plugin.Runtime;
using OptiKey.ET5.Plugin.Synthetic;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class SyntheticEndToEndTests
    {
        [Test]
        public void PointService_StreamsGazePointsAndShutsDown()
        {
            var syntheticProvider = new SyntheticGazeProvider(SyntheticTrajectoryPattern.CircularOrbit, intervalMs: 10);
            var mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));

            using (var service = new ET5PointService(syntheticProvider, mapper))
            {
                var receivedPoints = new List<Timestamped<Point>>();
                var pointEventReceived = new AutoResetEvent(false);

                EventHandler<Timestamped<Point>> handler = (s, p) =>
                {
                    lock (receivedPoints)
                    {
                        receivedPoints.Add(p);
                        if (receivedPoints.Count >= 5)
                        {
                            pointEventReceived.Set();
                        }
                    }
                };

                // Attaching Point listener initiates stream (Lazy Start)
                service.Point += handler;

                bool signaled = pointEventReceived.WaitOne(3000);
                Assert.IsTrue(signaled, "Timed out waiting for synthetic gaze points.");

                lock (receivedPoints)
                {
                    Assert.GreaterOrEqual(receivedPoints.Count, 5);
                    foreach (var pt in receivedPoints)
                    {
                        // Verify points fall within physical screen limits
                        Assert.GreaterOrEqual(pt.Value.X, 0);
                        Assert.LessOrEqual(pt.Value.X, 1920);
                        Assert.GreaterOrEqual(pt.Value.Y, 0);
                        Assert.LessOrEqual(pt.Value.Y, 1080);
                    }
                }

                // Removing listener stops stream
                service.Point -= handler;
                Assert.IsFalse(syntheticProvider.IsConnected);
            }
        }

        [Test]
        public void PointService_DispatchesErrorsCorrectly()
        {
            var syntheticProvider = new SyntheticGazeProvider();
            using (var service = new ET5PointService(syntheticProvider))
            {
                Exception capturedEx = null;
                var errorEventReceived = new AutoResetEvent(false);

                service.Error += (s, ex) =>
                {
                    capturedEx = ex;
                    errorEventReceived.Set();
                };

                // Attach to start
                EventHandler<Timestamped<Point>> dummyHandler = (s, p) => { };
                service.Point += dummyHandler;

                var expectedEx = new InvalidOperationException("Simulated tracker hardware fault");
                syntheticProvider.InjectError(expectedEx);

                bool signaled = errorEventReceived.WaitOne(2000);
                Assert.IsTrue(signaled, "Timed out waiting for Error event.");
                Assert.AreEqual(expectedEx, capturedEx);

                service.Point -= dummyHandler;
            }
        }

        [Test]
        public void PointService_MultipleDisposals_AreIdempotent()
        {
            var syntheticProvider = new SyntheticGazeProvider();
            var service = new ET5PointService(syntheticProvider);

            Assert.DoesNotThrow(() => service.Dispose());
            Assert.DoesNotThrow(() => service.Dispose());
            Assert.DoesNotThrow(() => service.Dispose());
        }

        [Test]
        public void FullProductionPipeline_EndToEnd_FromNativeCallbackToOptiKeyPointEmission()
        {
            var fakeRuntime = new NativeCallbackEmittingRuntime();
            var mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));
            var config = new PluginConfiguration(); // default: single-device auto connect + polling pump
            var provider = new TobiiGazeProvider(runtime: fakeRuntime, configuration: config);

            using (var service = new ET5PointService(provider, mapper))
            {
                var receivedPoints = new List<Timestamped<Point>>();
                var pointsArrived = new ManualResetEventSlim(false);

                EventHandler<Timestamped<Point>> handler = (s, pt) =>
                {
                    lock (receivedPoints)
                    {
                        receivedPoints.Add(pt);
                        if (receivedPoints.Count >= 5)
                        {
                            pointsArrived.Set();
                        }
                    }
                };

                // Attaching Point listener initiates full production stream
                service.Point += handler;

                bool arrived = pointsArrived.Wait(TimeSpan.FromSeconds(3));
                Assert.IsTrue(arrived, "Timed out waiting for production pipeline gaze points.");

                lock (receivedPoints)
                {
                    Assert.GreaterOrEqual(receivedPoints.Count, 5);
                    foreach (var pt in receivedPoints)
                    {
                        Assert.GreaterOrEqual(pt.Value.X, 0);
                        Assert.LessOrEqual(pt.Value.X, 1920);
                        Assert.GreaterOrEqual(pt.Value.Y, 0);
                        Assert.LessOrEqual(pt.Value.Y, 1080);
                    }
                }

                // Detaching listener stops stream cleanly
                service.Point -= handler;
                Assert.IsFalse(provider.IsConnected);
            }
        }

        private class NativeCallbackEmittingRuntime : ITobiiRuntime
        {
            private tobii_gaze_point_callback_t callback;
            private long timestampUs = 1000000;
            private float x = 0.5f;
            private float y = 0.5f;

            public bool Initialize() => true;
            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string> { "tobii-prx://simulated-et5-hardware" };
                return true;
            }
            public bool ConnectDevice(string url) => true;
            public bool DisconnectDevice() => true;
            public bool ReconnectDevice() => true;
            public bool SubscribeGaze(tobii_gaze_point_callback_t cb)
            {
                this.callback = cb;
                return true;
            }
            public bool UnsubscribeGaze()
            {
                this.callback = null;
                return true;
            }
            public tobii_error_t WaitForCallbacks()
            {
                Thread.Sleep(5);
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
            public tobii_error_t ProcessCallbacks()
            {
                if (callback != null)
                {
                    var pt = new tobii_gaze_point_t
                    {
                        timestamp_us = Interlocked.Add(ref timestampUs, 15000),
                        validity = tobii_validity_t.TOBII_VALIDITY_VALID,
                        position_x = x,
                        position_y = y
                    };
                    callback(ref pt, IntPtr.Zero);
                }
                return tobii_error_t.TOBII_ERROR_NO_ERROR;
            }
            public string GetLastErrorDescription() => "ok";
            public void Dispose() { }
        }
    }
}

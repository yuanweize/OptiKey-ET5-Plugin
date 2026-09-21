using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Mapping;
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
    }
}

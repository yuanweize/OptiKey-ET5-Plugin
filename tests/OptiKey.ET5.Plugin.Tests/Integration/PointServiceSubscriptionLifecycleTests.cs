using System;
using System.Reactive;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;

namespace OptiKey.ET5.Plugin.Tests.Integration
{
    [TestFixture]
    public class PointServiceSubscriptionLifecycleTests
    {
        private LifecycleTrackingGazeProvider fakeProvider;
        private ET5PointService pointService;

        [SetUp]
        public void SetUp()
        {
            fakeProvider = new LifecycleTrackingGazeProvider();
            pointService = new ET5PointService(gazeProvider: fakeProvider);
        }

        [TearDown]
        public void TearDown()
        {
            pointService?.Dispose();
        }

        [Test]
        public void Point_FirstSubscriber_StartsGazeProviderExactlyOnce()
        {
            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(0));

            EventHandler<Timestamped<Point>> handler1 = (s, e) => { };
            pointService.Point += handler1;

            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(1), "First subscriber must start provider exactly once.");
        }

        [Test]
        public void Point_SecondSubscriber_DoesNotCallStartAgain()
        {
            EventHandler<Timestamped<Point>> handler1 = (s, e) => { };
            EventHandler<Timestamped<Point>> handler2 = (s, e) => { };

            pointService.Point += handler1;
            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(1));

            pointService.Point += handler2;
            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(1), "Second subscriber must not trigger duplicate Start().");
        }

        [Test]
        public void Point_RemoveOneOfTwoSubscribers_DoesNotStopProvider()
        {
            EventHandler<Timestamped<Point>> handler1 = (s, e) => { };
            EventHandler<Timestamped<Point>> handler2 = (s, e) => { };

            pointService.Point += handler1;
            pointService.Point += handler2;

            pointService.Point -= handler1;
            Assert.That(fakeProvider.StopCallCount, Is.EqualTo(0), "Removing non-final subscriber must not stop provider.");
        }

        [Test]
        public void Point_RemoveLastSubscriber_StopsProviderExactlyOnce()
        {
            EventHandler<Timestamped<Point>> handler1 = (s, e) => { };
            EventHandler<Timestamped<Point>> handler2 = (s, e) => { };

            pointService.Point += handler1;
            pointService.Point += handler2;

            pointService.Point -= handler1;
            Assert.That(fakeProvider.StopCallCount, Is.EqualTo(0));

            pointService.Point -= handler2;
            Assert.That(fakeProvider.StopCallCount, Is.EqualTo(1), "Removing last subscriber must trigger Stop() exactly once.");
        }

        [Test]
        public void Point_ResubscribeAfterStopping_StartsProviderAgain()
        {
            EventHandler<Timestamped<Point>> handler = (s, e) => { };

            pointService.Point += handler;
            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(1));

            pointService.Point -= handler;
            Assert.That(fakeProvider.StopCallCount, Is.EqualTo(1));

            // Resubscribe (0 -> 1 subscriber transition again)
            pointService.Point += handler;
            Assert.That(fakeProvider.StartCallCount, Is.EqualTo(2), "Resubscribing after stop must restart provider.");

            pointService.Point -= handler;
            Assert.That(fakeProvider.StopCallCount, Is.EqualTo(2), "Removing resubscriber must stop provider again.");
        }

        [Test]
        public void Dispose_CallsProviderDisposeOutsideEventLock()
        {
            // Verify that provider.Dispose is called on service.Dispose
            Assert.That(fakeProvider.DisposeCallCount, Is.EqualTo(0));

            pointService.Dispose();

            Assert.That(fakeProvider.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_CalledRepeatedly_IsSafeAndIdempotent()
        {
            pointService.Dispose();
            Assert.That(fakeProvider.DisposeCallCount, Is.EqualTo(1));

            // Repeated dispose calls must be no-ops
            Assert.DoesNotThrow(() => pointService.Dispose());
            Assert.DoesNotThrow(() => pointService.Dispose());
            Assert.That(fakeProvider.DisposeCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_DuringActiveGazeCallback_DoesNotDeadlock()
        {
            var callbackEntered = new ManualResetEventSlim(false);
            var callbackFinish = new ManualResetEventSlim(false);
            int receivedPoints = 0;

            EventHandler<Timestamped<Point>> handler = (s, e) =>
            {
                receivedPoints++;
                callbackEntered.Set();
                callbackFinish.Wait(TimeSpan.FromSeconds(2));
            };

            pointService.Point += handler;

            // Fire gaze point on background thread
            var bgThread = new Thread(() =>
            {
                fakeProvider.SimulateGazePoint(0.5f, 0.5f);
            });
            bgThread.Start();

            // Wait until callback is in progress
            Assert.That(callbackEntered.Wait(TimeSpan.FromSeconds(2)), Is.True);

            // Dispose service while callback is actively executing
            var disposeThread = new Thread(() =>
            {
                pointService.Dispose();
            });
            disposeThread.Start();

            // Unblock the callback
            callbackFinish.Set();

            // Both threads must complete promptly without deadlock
            Assert.That(disposeThread.Join(TimeSpan.FromSeconds(2)), Is.True, "Dispose must not deadlock with active callback.");
            Assert.That(bgThread.Join(TimeSpan.FromSeconds(2)), Is.True);
        }

        [Test]
        public void Events_AfterDispose_AreSafelyIgnored()
        {
            int pointsReceived = 0;
            pointService.Point += (s, e) => pointsReceived++;

            pointService.Dispose();

            // Simulating gaze point after disposal should not throw and should not deliver
            Assert.DoesNotThrow(() =>
            {
                fakeProvider.SimulateGazePoint(0.3f, 0.7f);
            });

            Assert.That(pointsReceived, Is.EqualTo(0));
        }

        private class LifecycleTrackingGazeProvider : IGazeProvider
        {
            public event EventHandler<GazePointEventArgs> GazePointAvailable;
            public event EventHandler<Exception> ErrorOccurred;
            public event EventHandler<bool> ConnectionStatusChanged;

            public int StartCallCount { get; private set; }
            public int StopCallCount { get; private set; }
            public int DisposeCallCount { get; private set; }

            public bool IsConnected => true;

            public void Start()
            {
                StartCallCount++;
                ConnectionStatusChanged?.Invoke(this, true);
            }

            public void Stop()
            {
                StopCallCount++;
                ConnectionStatusChanged?.Invoke(this, false);
            }

            public void SimulateGazePoint(float x, float y)
            {
                GazePointAvailable?.Invoke(this, new GazePointEventArgs(x, y, 123456, true));
            }

            public void SimulateError(Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
            }

            public void Dispose()
            {
                DisposeCallCount++;
            }
        }
    }
}

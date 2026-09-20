using System;
using System.Collections.Generic;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Runtime;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class ProviderLifecycleTests
    {
        [Test]
        public void StopBeforeStart_IsIdempotent()
        {
            using (var provider = CreateProvider())
            {
                Assert.DoesNotThrow(() => provider.Stop());
                Assert.DoesNotThrow(() => provider.Stop());
            }
        }

        [Test]
        public void DisposeBeforeStart_IsIdempotent()
        {
            var provider = CreateProvider();

            Assert.DoesNotThrow(() => provider.Dispose());
            Assert.DoesNotThrow(() => provider.Dispose());
        }

        [Test]
        public void RepeatedStartAndStop_DoesNotThrow()
        {
            using (var provider = CreateProvider())
            {
                Assert.DoesNotThrow(() => provider.Start());
                Assert.DoesNotThrow(() => provider.Start());
                Assert.DoesNotThrow(() => provider.Stop());
                Assert.DoesNotThrow(() => provider.Stop());
            }
        }

        private static TobiiGazeProvider CreateProvider()
        {
            return new TobiiGazeProvider(
                new FailingRuntime(),
                new ImmediateReconnectPolicy());
        }

        private sealed class ImmediateReconnectPolicy : IReconnectPolicy
        {
            public int GetNextDelayMilliseconds(int attemptNumber)
            {
                return 1;
            }

            public void Reset()
            {
            }
        }

        private sealed class FailingRuntime : ITobiiRuntime
        {
            public bool Initialize()
            {
                return false;
            }

            public bool EnumerateDevices(out List<string> urls)
            {
                urls = new List<string>();
                return false;
            }

            public bool ConnectDevice(string url)
            {
                return false;
            }

            public bool DisconnectDevice()
            {
                return true;
            }

            public bool ReconnectDevice()
            {
                return false;
            }

            public bool SubscribeGaze(tobii_gaze_point_callback_t callback)
            {
                return false;
            }

            public bool UnsubscribeGaze()
            {
                return true;
            }

            public tobii_error_t WaitForCallbacks()
            {
                return tobii_error_t.TOBII_ERROR_NOT_AVAILABLE;
            }

            public tobii_error_t ProcessCallbacks()
            {
                return tobii_error_t.TOBII_ERROR_NOT_AVAILABLE;
            }

            public bool TryGetDeviceInfo(out tobii_device_info_t info)
            {
                info = default(tobii_device_info_t);
                return false;
            }

            public string GetLastErrorDescription()
            {
                return "synthetic failure";
            }

            public void Dispose()
            {
            }
        }
    }
}

using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class ReconnectPolicyTests
    {
        [Test]
        public void Delays_GrowExponentially()
        {
            var policy = new ExponentialBackoffReconnectPolicy(baseDelayMs: 500, maxDelayMs: 10000, maxJitterMs: 0);

            Assert.AreEqual(500, policy.GetNextDelayMilliseconds(0));   // 500 * 2^0
            Assert.AreEqual(1000, policy.GetNextDelayMilliseconds(1));  // 500 * 2^1
            Assert.AreEqual(2000, policy.GetNextDelayMilliseconds(2));  // 500 * 2^2
            Assert.AreEqual(4000, policy.GetNextDelayMilliseconds(3));  // 500 * 2^3
            Assert.AreEqual(8000, policy.GetNextDelayMilliseconds(4));  // 500 * 2^4
        }

        [Test]
        public void Delays_AreCappedAtMaxDelay()
        {
            var policy = new ExponentialBackoffReconnectPolicy(baseDelayMs: 500, maxDelayMs: 10000, maxJitterMs: 0);

            Assert.AreEqual(10000, policy.GetNextDelayMilliseconds(5)); // 500 * 32 = 16000 -> clamped to 10000
            Assert.AreEqual(10000, policy.GetNextDelayMilliseconds(10));
            Assert.AreEqual(10000, policy.GetNextDelayMilliseconds(50));
        }

        [Test]
        public void Jitter_IsBounded()
        {
            var policy = new ExponentialBackoffReconnectPolicy(baseDelayMs: 500, maxDelayMs: 10000, maxJitterMs: 250);

            for (int i = 0; i < 20; i++)
            {
                int delay = policy.GetNextDelayMilliseconds(1);
                // Expected range for attempt 1: 1000 to 1250
                Assert.GreaterOrEqual(delay, 1000);
                Assert.LessOrEqual(delay, 1250);
            }
        }

        [Test]
        public void NegativeAttempt_IsHandledGracefully()
        {
            var policy = new ExponentialBackoffReconnectPolicy(baseDelayMs: 500, maxDelayMs: 10000, maxJitterMs: 0);
            Assert.AreEqual(500, policy.GetNextDelayMilliseconds(-5));
        }
    }
}

using System;
using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Mapping;

namespace OptiKey.ET5.Plugin.Tests
{
    /// <summary>
    /// Tests robust handling of malformed, out-of-bounds, NaN, and invalid gaze data.
    /// AAC users cannot afford crashes caused by erratic tracker hardware or reflection glitches.
    /// </summary>
    [TestFixture]
    public class MalformedGazeTests
    {
        private ScreenCoordinateMapper mapper;

        [SetUp]
        public void SetUp()
        {
            mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));
        }

        [Test]
        public void NaN_X_Coordinate_IsRejected()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(float.NaN, 0.5f, true, out Point pt);
            Assert.That(mapped, Is.False);
            Assert.That(pt, Is.EqualTo(default(Point)));
        }

        [Test]
        public void NaN_Y_Coordinate_IsRejected()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(0.5f, float.NaN, true, out Point pt);
            Assert.That(mapped, Is.False);
        }

        [Test]
        public void PositiveInfinity_IsRejected()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(float.PositiveInfinity, 0.5f, true, out Point pt);
            Assert.That(mapped, Is.False);
        }

        [Test]
        public void NegativeInfinity_IsRejected()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(0.5f, float.NegativeInfinity, true, out Point pt);
            Assert.That(mapped, Is.False);
        }

        [Test]
        public void InvalidTrackingFlag_IsRejectedEvenWithValidCoordinates()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(0.5f, 0.5f, false, out Point pt);
            Assert.That(mapped, Is.False);
        }

        [Test]
        public void SubZeroCoordinates_AreClampedToZero()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(-0.25f, -0.1f, true, out Point pt);
            Assert.That(mapped, Is.True);
            Assert.That(pt.X, Is.EqualTo(0));
            Assert.That(pt.Y, Is.EqualTo(0));
        }

        [Test]
        public void GreaterThanOneCoordinates_AreClampedToMaximum()
        {
            bool mapped = mapper.TryMapToPhysicalPixel(1.5f, 2.0f, true, out Point pt);
            Assert.That(mapped, Is.True);
            Assert.That(pt.X, Is.EqualTo(1920));
            Assert.That(pt.Y, Is.EqualTo(1080));
        }

        [Test]
        public void NullArgs_ReturnsFalseGracefully()
        {
            bool mapped = mapper.TryMap(null, out Point pt);
            Assert.That(mapped, Is.False);
            Assert.That(pt, Is.EqualTo(default(Point)));
        }

        [Test]
        public void GazePointEventArgs_PreservesExtremeTimestamps()
        {
            long maxTimestamp = long.MaxValue;
            var args = new GazePointEventArgs(0.5f, 0.5f, maxTimestamp, true);
            Assert.That(args.TimestampMicroseconds, Is.EqualTo(maxTimestamp));

            long minTimestamp = 0;
            var argsZero = new GazePointEventArgs(0.5f, 0.5f, minTimestamp, false);
            Assert.That(argsZero.TimestampMicroseconds, Is.EqualTo(minTimestamp));
            Assert.That(argsZero.IsValid, Is.False);
        }
    }
}

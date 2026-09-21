using System.Windows;
using NUnit.Framework;
using OptiKey.ET5.Plugin.Core;
using OptiKey.ET5.Plugin.Mapping;

namespace OptiKey.ET5.Plugin.Tests
{
    [TestFixture]
    public class CoordinateMapperTests
    {
        [Test]
        public void StandardFullHd_MapsAccurately()
        {
            var metrics = new DisplayMetrics(0, 0, 1920, 1080);
            var mapper = new ScreenCoordinateMapper(metrics);

            // Origin (Top-Left)
            Assert.IsTrue(mapper.TryMapToPhysicalPixel(0.0f, 0.0f, true, out Point pTopLeft));
            Assert.AreEqual(0.0, pTopLeft.X, 0.001);
            Assert.AreEqual(0.0, pTopLeft.Y, 0.001);

            // Bottom-Right
            Assert.IsTrue(mapper.TryMapToPhysicalPixel(1.0f, 1.0f, true, out Point pBottomRight));
            Assert.AreEqual(1920.0, pBottomRight.X, 0.001);
            Assert.AreEqual(1080.0, pBottomRight.Y, 0.001);

            // Screen Center
            Assert.IsTrue(mapper.TryMapToPhysicalPixel(0.5f, 0.5f, true, out Point pCenter));
            Assert.AreEqual(960.0, pCenter.X, 0.001);
            Assert.AreEqual(540.0, pCenter.Y, 0.001);
        }

        [Test]
        public void QuadHdAnd4k_MapAccurately()
        {
            // 2560x1440
            var mapper1440p = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 2560, 1440));
            Assert.IsTrue(mapper1440p.TryMapToPhysicalPixel(0.5f, 0.5f, true, out Point p1440));
            Assert.AreEqual(1280.0, p1440.X, 0.001);
            Assert.AreEqual(720.0, p1440.Y, 0.001);

            // 3840x2160 (4K)
            var mapper4k = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 3840, 2160));
            Assert.IsTrue(mapper4k.TryMapToPhysicalPixel(0.25f, 0.75f, true, out Point p4k));
            Assert.AreEqual(960.0, p4k.X, 0.001);
            Assert.AreEqual(1620.0, p4k.Y, 0.001);
        }

        [Test]
        public void OffsetDisplay_MapsCorrectly()
        {
            // Secondary display offset at X=1920, Y=0, Size=1920x1080
            var metrics = new DisplayMetrics(1920, 0, 1920, 1080);
            var mapper = new ScreenCoordinateMapper(metrics);

            Assert.IsTrue(mapper.TryMapToPhysicalPixel(0.0f, 0.0f, true, out Point pStart));
            Assert.AreEqual(1920.0, pStart.X, 0.001);
            Assert.AreEqual(0.0, pStart.Y, 0.001);

            Assert.IsTrue(mapper.TryMapToPhysicalPixel(1.0f, 1.0f, true, out Point pEnd));
            Assert.AreEqual(3840.0, pEnd.X, 0.001);
            Assert.AreEqual(1080.0, pEnd.Y, 0.001);
        }

        [Test]
        public void InvalidGazeData_IsRejected()
        {
            var mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));

            // isValid = false
            Assert.IsFalse(mapper.TryMapToPhysicalPixel(0.5f, 0.5f, false, out Point _));

            // NaN values
            Assert.IsFalse(mapper.TryMapToPhysicalPixel(float.NaN, 0.5f, true, out Point _));
            Assert.IsFalse(mapper.TryMapToPhysicalPixel(0.5f, float.NaN, true, out Point _));

            // Infinity values
            Assert.IsFalse(mapper.TryMapToPhysicalPixel(float.PositiveInfinity, 0.5f, true, out Point _));
            Assert.IsFalse(mapper.TryMapToPhysicalPixel(0.5f, float.NegativeInfinity, true, out Point _));
        }

        [Test]
        public void OutOfRangeCoordinates_AreClamped()
        {
            var mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));

            // Negative normalized coordinate clamped to 0.0
            Assert.IsTrue(mapper.TryMapToPhysicalPixel(-0.2f, 0.5f, true, out Point pClampedLeft));
            Assert.AreEqual(0.0, pClampedLeft.X, 0.001);

            // Greater than 1.0 normalized coordinate clamped to 1.0
            Assert.IsTrue(mapper.TryMapToPhysicalPixel(1.25f, 0.5f, true, out Point pClampedRight));
            Assert.AreEqual(1920.0, pClampedRight.X, 0.001);
        }

        [Test]
        public void GazePointEventArgs_Integration()
        {
            var mapper = new ScreenCoordinateMapper(new DisplayMetrics(0, 0, 1920, 1080));
            var args = new GazePointEventArgs(0.5f, 0.5f, 1234567, true);

            Assert.IsTrue(mapper.TryMap(args, out Point pt));
            Assert.AreEqual(960.0, pt.X, 0.001);
            Assert.AreEqual(540.0, pt.Y, 0.001);

            Assert.IsFalse(mapper.TryMap(null, out Point _));
        }
    }
}

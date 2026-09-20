using System;
using System.Windows;
using OptiKey.ET5.Plugin.Core;

namespace OptiKey.ET5.Plugin.Mapping
{
    /// <summary>
    /// Transforms normalized eye tracker coordinates [0.0, 1.0] into physical screen pixel
    /// coordinates compatible with OptiKey's PointToKeyValueMap hit-testing (ADR-007).
    /// </summary>
    public class ScreenCoordinateMapper
    {
        private DisplayMetrics metrics;
        private readonly object syncLock = new object();

        public ScreenCoordinateMapper(DisplayMetrics displayMetrics = null)
        {
            this.metrics = displayMetrics ?? DisplayMetrics.GetPrimaryDisplayMetrics();
        }

        public DisplayMetrics CurrentMetrics
        {
            get
            {
                lock (syncLock)
                {
                    return metrics;
                }
            }
        }

        /// <summary>
        /// Updates the target display metrics (e.g. following display resolution changes).
        /// </summary>
        public void UpdateDisplayMetrics(DisplayMetrics newMetrics)
        {
            if (newMetrics == null) throw new ArgumentNullException(nameof(newMetrics));
            lock (syncLock)
            {
                metrics = newMetrics;
            }
        }

        /// <summary>
        /// Attempts to map normalized gaze coordinates to physical screen coordinates.
        /// Returns true if successful; false if input data is invalid or untracked.
        /// </summary>
        public bool TryMapToPhysicalPixel(float normalizedX, float normalizedY, bool isValid, out Point mappedPoint)
        {
            mappedPoint = default(Point);

            if (!isValid)
            {
                return false;
            }

            if (float.IsNaN(normalizedX) || float.IsInfinity(normalizedX) ||
                float.IsNaN(normalizedY) || float.IsInfinity(normalizedY))
            {
                return false;
            }

            // Clamp normalized range to [0.0, 1.0]
            float clampedX = Math.Max(0.0f, Math.Min(1.0f, normalizedX));
            float clampedY = Math.Max(0.0f, Math.Min(1.0f, normalizedY));

            DisplayMetrics m;
            lock (syncLock)
            {
                m = metrics;
            }

            double pixelX = m.Left + (clampedX * m.Width);
            double pixelY = m.Top + (clampedY * m.Height);

            mappedPoint = new Point(pixelX, pixelY);
            return true;
        }

        /// <summary>
        /// Maps from a GazePointEventArgs directly.
        /// </summary>
        public bool TryMap(GazePointEventArgs args, out Point mappedPoint)
        {
            if (args == null)
            {
                mappedPoint = default(Point);
                return false;
            }

            return TryMapToPhysicalPixel(args.NormalizedX, args.NormalizedY, args.IsValid, out mappedPoint);
        }
    }
}

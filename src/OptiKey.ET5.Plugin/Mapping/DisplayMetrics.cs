using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace OptiKey.ET5.Plugin.Mapping
{
    /// <summary>
    /// Represents physical screen boundary metrics for coordinate conversion.
    /// </summary>
    public class DisplayMetrics
    {
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public double Left { get; }
        public double Top { get; }
        public double Width { get; }
        public double Height { get; }

        public DisplayMetrics(double left, double top, double width, double height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be positive.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be positive.");

            Left = left;
            Top = top;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Retrieves the primary display physical metrics using Win32 API.
        /// </summary>
        public static DisplayMetrics GetPrimaryDisplayMetrics()
        {
            try
            {
                int width = GetSystemMetrics(SM_CXSCREEN);
                int height = GetSystemMetrics(SM_CYSCREEN);

                if (width > 0 && height > 0)
                {
                    return new DisplayMetrics(0, 0, width, height);
                }
            }
            catch
            {
                // Fallback handled below
            }

            // Fallback default: Full HD
            return new DisplayMetrics(0, 0, 1920, 1080);
        }
    }
}

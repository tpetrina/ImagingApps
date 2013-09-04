using System.Windows;

namespace AdvancedPhotoFilter.Common
{
    internal static class MathEx
    {
        public static int Clamp(int value, int min, int max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        /// <summary>
        /// Returns distance squared.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance2(Point p1, Point p2)
        {
            return (p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y);
        }

        /// <summary>
        /// Transforms image from RGB coordinates to Y'UV.
        /// </summary>
        /// <param name="pixel"></param>
        /// <returns></returns>
        public static Point GetUV(int pixel)
        {
            var r = (pixel >> 16) & 0xFF;
            var g = (pixel >> 8) & 0xFF;
            var b = (pixel) & 0xFF;

            var u = -0.14713 * r + -0.28886 * g + 0.436 * b;
            var v = 0.615 * r + -0.51499 * g + -0.10001 * b;

            return new Point(u, v);
        }
    }
}

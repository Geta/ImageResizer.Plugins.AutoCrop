using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class PointExtensions
    {
        public static Point Invert(this Point point)
        {
            return new Point(-point.X, -point.Y);
        }
    }
}

using System.Drawing;

namespace AutoCrop.Core.Extensions
{
    public static class ColorExtensions
    {
        public static int ToColorBucket(this Color color)
        {
            return (int)((0.299 * color.R + 0.587 * color.G + 0.114 * color.B) * color.A * 0.0003921568627451);
        }
    }
}

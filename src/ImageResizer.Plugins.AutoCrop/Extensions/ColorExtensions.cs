using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class ColorExtensions
    {
        private static readonly int _maxBuckets = 50;
        private static readonly double _bucketPrecision = _maxBuckets / (double)(byte.MaxValue * byte.MaxValue);

        public static int ToColorBucket(this Color color)
        {
            return (int)((0.299 * color.R + 0.587 * color.G + 0.114 * color.B) * color.A * _bucketPrecision);
        }

        public static int GetMaxColorBuckets()
        {
            return _maxBuckets;
        }
    }
}

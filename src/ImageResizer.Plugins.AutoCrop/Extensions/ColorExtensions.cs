using System;
using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class ColorExtensions
    {
        private static readonly byte _maxBuckets = 50;
        private static readonly double _bytePrecision = 1 / (double)byte.MaxValue;
        private static readonly double _bucketPrecision = _maxBuckets / (double)byte.MaxValue;

        public static byte ToColorBucket(this Color color)
        {
            return (byte)Math.Round(ToGrayscale(color) * _bucketPrecision);
        }

        public static byte ToGrayscale(this Color color)
        {
            var mix = (uint)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);

            return (byte)Math.Min(byte.MaxValue, mix * (color.A * _bytePrecision) + (byte.MaxValue - color.A));
        }

        public static byte GetMaxColorBuckets()
        {
            return _maxBuckets;
        }
    }
}

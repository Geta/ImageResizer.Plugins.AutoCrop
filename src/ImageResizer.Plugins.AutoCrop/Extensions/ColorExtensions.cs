using System;
using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class ColorExtensions
    {
        private static readonly byte _maxBuckets = 20;
        private static readonly double _bucketPrecision = (_maxBuckets + 1) / (double)byte.MaxValue;

        public static byte ToColorBucket(this Color color)
        {
            return (byte)Math.Min(_maxBuckets, Math.Floor(ToGrayscale(color) * _bucketPrecision));
        }

        public static byte ToGrayscale(this Color color)
        {
            var mix = (uint)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);

            return (byte)Math.Min(byte.MaxValue, mix * (color.A * Constants.BytePrecision) + (byte.MaxValue - color.A));
        }

        public static byte GetMaxColorBuckets()
        {
            return _maxBuckets;
        }
    }
}

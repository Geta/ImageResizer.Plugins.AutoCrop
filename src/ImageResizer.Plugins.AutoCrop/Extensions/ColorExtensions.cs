using System;
using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class ColorExtensions
    {
        private static readonly byte _maxBuckets = 17;
        private static readonly double _bucketPrecision = (_maxBuckets + 1) / (double)byte.MaxValue;
        private static readonly double _bucketRatio = byte.MaxValue / _maxBuckets;

        public static byte ToColorBucket(this Color color)
        {
            unchecked
            {
                return (byte)Math.Min(_maxBuckets, Math.Floor(ToGrayscale(color) * _bucketPrecision));
            }
        }

        public static byte ToColorValue(this byte bucket)
        {
            unchecked
            {
                return (byte)Math.Min(byte.MaxValue, bucket * _bucketRatio);
            }
        }
        
        public static Color ToColor(this byte bucket)
        {
            var v = bucket.ToColorValue();
            return Color.FromArgb(v, v, v);
        }

        public static byte ToGrayscale(this Color color)
        {
            unchecked
            {
                var mix = (byte)(0.299 * color.R + 0.587 * color.G + 0.114 * color.B);
                if (color.A == byte.MaxValue)
                    return mix;

                return (byte)Math.Min(byte.MaxValue, mix * (color.A * Constants.BytePrecision) + (byte.MaxValue - color.A));
            }            
        }

        public static byte GetMaxColorBuckets()
        {
            return _maxBuckets;
        }

        public static double GetBucketRatio()
        {
            return _bucketRatio;
        }
    }
}

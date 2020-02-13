using ImageResizer.Plugins.AutoCrop.Extensions;
using ImageResizer.Plugins.AutoCrop.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class SnakeAnalyzer
    {
        public readonly Rectangle Border;
        public readonly int BitsPerPixel;
        protected readonly byte ColorThreshold;

        public SnakeAnalyzer(BitmapData bitmap) : this(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height))
        {
            
        }

        public SnakeAnalyzer(BitmapData bitmap, Rectangle rectangle)
        {
            var imageDimensions = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            if (!imageDimensions.Contains(rectangle))
                throw new ArgumentException("Provided rectangle cannot be outside image bounds");

            Border = rectangle;
            BitsPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            ColorThreshold = 35;

            SnakeAnalysisResult result;

            switch (BitsPerPixel)
            {
                case 3: result = AnalyzeRgb(bitmap, rectangle); break;
                case 4: result = AnalyzeRgba(bitmap, rectangle); break;
                default: result = new SnakeAnalysisResult(); break;
            }
        }       

        private unsafe SnakeAnalysisResult AnalyzeRgba(BitmapData bitmap, Rectangle rect)
        {
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;
            var l = rect.Width * 2 + rect.Height * 2;

            var colors = new Dictionary<Color, int>(ColorThreshold);
            var buckets = new Dictionary<int, int>(ColorExtensions.GetMaxColorBuckets());
            var difference = (byte)0;

            unchecked
            {
                var cm = new byte[3][]
                {
                    new byte[4],
                    GetValue(s0, s, -1, rect, l, 4),
                    GetValue(s0, s, 0, rect, l, 4),
                };

                for (var p = 0; p < l; p++)
                {
                    cm[1] = cm[2];
                    cm[2] = cm[3];
                    cm[3] = GetValue(s0, s, p + 1, rect, l, 4);

                    var d = GetDiffRgba(cm);
                    if (d > difference) difference = d;

                    var c = Color.FromArgb(cm[1][3], cm[1][0], cm[1][1], cm[1][2]);
                    var cb = c.ToColorBucket();

                    if (buckets.ContainsKey(cb))
                    {
                        buckets[cb]++;
                    }
                    else
                    {
                        buckets.Add(cb, 1);
                    }

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < ColorThreshold)
                    {
                        colors.Add(c, 1);
                    }
                }
            }

            return new SnakeAnalysisResult(colors, buckets, difference);
        }

        private unsafe SnakeAnalysisResult AnalyzeRgb(BitmapData bitmap, Rectangle rect)
        {
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;
            var l = rect.Width * 2 + rect.Height * 2;

            var colors = new Dictionary<Color, int>(ColorThreshold);
            var buckets = new Dictionary<int, int>(ColorExtensions.GetMaxColorBuckets());
            var difference = (byte)0;

            unchecked
            {
                var cm = new byte[3][] 
                { 
                    new byte[3],
                    GetValue(s0, s, -1, rect, l, 3), 
                    GetValue(s0, s, 0, rect, l, 3),
                };

                for (var p = 0; p < l; p++)
                {
                    cm[1] = cm[2];
                    cm[2] = cm[3];
                    cm[3] = GetValue(s0, s, p + 1, rect, l, 3);

                    var d = GetDiffRgb(cm);
                    if (d > difference) difference = d;

                    var c = Color.FromArgb(cm[1][0], cm[1][1], cm[1][2]);
                    var cb = c.ToColorBucket();

                    if (buckets.ContainsKey(cb))
                    {
                        buckets[cb]++;
                    }
                    else
                    {
                        buckets.Add(cb, 1);
                    }

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < ColorThreshold)
                    {
                        colors.Add(c, 1);
                    }
                }
            }

            return new SnakeAnalysisResult(colors, buckets, difference);
        }

        private byte GetDiffRgb(byte[][] matrix)
        {
            var n = (byte)(0.299 * matrix[0][0] + 0.587 * matrix[0][1] + 0.114 * matrix[0][2]);
            var p = (byte)(0.299 * matrix[2][0] + 0.587 * matrix[2][1] + 0.114 * matrix[2][2]);

            return (byte)((p - n) * 0.5);
        }

        private byte GetDiffRgba(byte[][] matrix)
        {
            var bn = byte.MaxValue - matrix[0][3];
            var bp = byte.MaxValue - matrix[2][3];
            var n = (byte)(0.299 * Math.Min(matrix[0][0] + bn, 255) + 0.587 * Math.Min(matrix[0][1] + bn, 255) + 0.114 * Math.Min(matrix[0][2] + bn, 255));
            var p = (byte)(0.299 * Math.Min(matrix[2][0] + bp, 255) + 0.587 * Math.Min(matrix[2][1] + bp, 255) + 0.114 * Math.Min(matrix[2][2] + bp, 255));

            return (byte)((p - n) * 0.5);
        }

        private unsafe byte[] GetValues(byte* scan, int stride, int x, int y, byte bits)
        {
            var r = scan + y * stride;
            var p = x * bits;
            var data = new byte[bits];

            for (var i = 0; i < bits; i++)
            {
                data[i] = r[p + i];
            }

            return data;
        }

        private unsafe byte[] GetValue(byte* scan, int stride, int position, Rectangle bounds, int length, byte bits)
        {
            while (position < 0) position += length;
            while (position > length - 1) position -= length;

            // along top border
            if (position < bounds.Width) return GetValues(scan, stride, bounds.Left + position, bounds.Top, bits);

            // along right border
            if (position < bounds.Width + bounds.Height) return GetValues(scan, stride, bounds.Right - 1, bounds.Top + (position - bounds.Width), bits);

            // along bottom
            if (position < bounds.Width * 2 + bounds.Height) return GetValues(scan, stride, bounds.Right - (position - bounds.Width - bounds.Height), bounds.Bottom, bits);

            // along left border
            return GetValues(scan, stride, bounds.Left, bounds.Bottom - (position - bounds.Width * 2 - bounds.Height), bits);
        }
    }
}

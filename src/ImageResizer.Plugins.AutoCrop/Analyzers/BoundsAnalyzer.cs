using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class BoundsAnalyzer
    {
        public readonly Rectangle BoundingBox;
        public readonly bool FoundBoundingBox;

        public BoundsAnalyzer(BitmapData bitmap, int threshold)
        {
            var imageBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var borderAnalyzer = new BorderAnalyzer(bitmap, threshold);

            if (borderAnalyzer.BorderIsDirty)
            {
                BoundingBox = imageBox;
                FoundBoundingBox = false;
            }
            else
            {
                if (borderAnalyzer.BitsPerPixel == 3)
                {
                    BoundingBox = GetBoundingBoxForContentRgb(bitmap, borderAnalyzer.BackgroundColor, threshold);
                }
                else
                {
                    BoundingBox = GetBoundingBoxForContentArgb(bitmap, borderAnalyzer.BackgroundColor, threshold);
                }

                FoundBoundingBox = !imageBox.Equals(BoundingBox);
            }
        }

        private unsafe Rectangle GetBoundingBoxForContentRgb(BitmapData bitmap, Color backgroundColor, int threshold)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var xn = w;
            var xm = 0;
            var yn = h;
            var ym = 0;

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;

                    for (var x = 0; x < w; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];

                        var bd = Math.Abs(b - backgroundColor.B);
                        var gd = Math.Abs(g - backgroundColor.G);
                        var rd = Math.Abs(r - backgroundColor.R);

                        if (0.299 * rd + 0.587 * gd + 0.114 * bd <= threshold)
                            continue;

                        if (x < xn) xn = x;
                        if (x > xm) xm = x;
                        if (y < yn) yn = y;
                        if (y > ym) ym = y;
                    }
                }
            }

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }
    
        private unsafe Rectangle GetBoundingBoxForContentArgb(BitmapData bitmap, Color backgroundColor, int threshold)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*) bitmap.Scan0;

            var xn = w;
            var xm = 0;
            var yn = h;
            var ym = 0;

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;

                    for (var x = 0; x < w; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];
                        var a = row[p + 3] * 0.003921568627451;

                        var bd = Math.Abs(b - backgroundColor.B) * a;
                        var gd = Math.Abs(g - backgroundColor.G) * a;
                        var rd = Math.Abs(r - backgroundColor.R) * a;

                        if (0.299 * rd + 0.587 * gd + 0.114 * bd <= threshold)
                            continue;

                        if (x < xn) xn = x;
                        if (x > xm) xm = x;
                        if (y < yn) yn = y;
                        if (y > ym) ym = y;
                    }
                }
            }

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }
    }
}

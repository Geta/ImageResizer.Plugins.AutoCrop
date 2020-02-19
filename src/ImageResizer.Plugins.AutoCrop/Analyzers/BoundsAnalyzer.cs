using ImageResizer.Plugins.AutoCrop.Extensions;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class BoundsAnalyzer
    {
        public readonly bool FoundBoundingBox;
        public readonly Rectangle BoundingBox;
        public readonly BorderAnalyzer BorderAnalysis;

        public BoundsAnalyzer(BitmapData bitmap, int colorThreshold, float bucketTreshold)
        {
            var outerBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageBox = outerBox;

            BorderAnalysis = new BorderAnalyzer(bitmap, imageBox, colorThreshold, bucketTreshold);

            if (BorderAnalysis.Failed)
            {
                colorThreshold = (int)Math.Round(colorThreshold * 0.5);
                bucketTreshold = 1.0f;
                imageBox = imageBox.Contract(10);

                var additionalAnalysis = new BorderAnalyzer(bitmap, imageBox, colorThreshold, bucketTreshold);
                if (!additionalAnalysis.Failed)
                {
                    BorderAnalysis = additionalAnalysis;
                }
            }

            if (BorderAnalysis.Failed)
            {
                BoundingBox = outerBox;
                FoundBoundingBox = false;
            }
            else
            {
                if (BorderAnalysis.BitsPerPixel == 3)
                {
                    BoundingBox = GetBoundingBoxForContentRgb(bitmap, BorderAnalysis.Rectangle, BorderAnalysis.BackgroundColor, colorThreshold);
                }
                else
                {
                    BoundingBox = GetBoundingBoxForContentArgb(bitmap, BorderAnalysis.Rectangle, BorderAnalysis.BackgroundColor, colorThreshold);
                }

                FoundBoundingBox = ValidateRectangle(BoundingBox);
            }
        }

        private bool ValidateRectangle(Rectangle rectangle)
        {
            if (rectangle == null) return false;
            if (rectangle.Width < 3) return false;
            if (rectangle.Height < 3)  return false;

            return true;
        }

        private unsafe Rectangle GetBoundingBoxForContentRgb(BitmapData bitmap, Rectangle rectangle, Color backgroundColor, int threshold)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = rectangle.Bottom;
            var w = rectangle.Right;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var xn = w;
            var xm = rectangle.X;
            var yn = h;
            var ym = rectangle.Y;

            unchecked
            {
                for (var y = rectangle.Y; y < h; y++)
                {
                    var row = s0 + y * s;

                    for (var x = rectangle.X; x < w; x++)
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
    
        private unsafe Rectangle GetBoundingBoxForContentArgb(BitmapData bitmap, Rectangle rectangle, Color backgroundColor, int threshold)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = rectangle.Bottom;
            var w = rectangle.Right;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var xn = w;
            var xm = rectangle.X;
            var yn = h;
            var ym = rectangle.Y;

            unchecked
            {
                for (var y = rectangle.Y; y < h; y++)
                {
                    var row = s0 + y * s;

                    for (var x = rectangle.X; x < w; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];
                        var a = row[p + 3];
                        var ac = a * 0.003921568627451;

                        var bd = Math.Abs(b - backgroundColor.B) * ac;
                        var gd = Math.Abs(g - backgroundColor.G) * ac;
                        var rd = Math.Abs(r - backgroundColor.R) * ac;

                        if (0.299 * rd + 0.587 * gd + 0.114 * bd <= threshold)
                        {
                            var ad = Math.Abs(a - backgroundColor.A);
                            if (ad < threshold)
                            {
                                continue;
                            }
                        }                        

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

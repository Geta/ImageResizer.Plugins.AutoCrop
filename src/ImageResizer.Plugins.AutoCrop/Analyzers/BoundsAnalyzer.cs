﻿using ImageResizer.Plugins.AutoCrop.Extensions;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Plugins.AutoCrop.Detection;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class BoundsAnalyzer : ICropAnalyzer
    {
        private readonly bool _foundBoundingBox;
        private readonly Rectangle _boundingBox;
        private readonly ICropAnalysis _cropAnalysis;

        public BoundsAnalyzer(BitmapData bitmap, int colorThreshold, float bucketTreshold)
        {
            var outerBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var imageBox = outerBox;

            var borderInspection = new BorderInspector(bitmap, imageBox, colorThreshold, bucketTreshold);
            if (borderInspection.Failed)
            {
                colorThreshold = (int)Math.Round(colorThreshold * 0.5);
                bucketTreshold = 1.0f;
                imageBox = imageBox.Contract(10);

                var additionalInspection = new BorderInspector(bitmap, imageBox, colorThreshold, bucketTreshold);
                if (!additionalInspection.Failed)
                {
                    borderInspection = additionalInspection;
                }
            }

            if (borderInspection.Failed)
            {
                _boundingBox = outerBox;
                _foundBoundingBox = false;
            }
            else
            {
                if (borderInspection.BytesPerPixel == 3)
                {
                    _boundingBox = GetBoundingBoxForContentRgb(bitmap, borderInspection.Rectangle, borderInspection.BackgroundColor, colorThreshold);
                }
                else
                {
                    _boundingBox = GetBoundingBoxForContentArgb(bitmap, borderInspection.Rectangle, borderInspection.BackgroundColor, colorThreshold);
                }

                _foundBoundingBox = ValidateRectangle(_boundingBox);
            }

            _cropAnalysis = new CropAnalysis
            {
                Background = borderInspection.BackgroundColor,
                BoundingBox = _boundingBox,
                Success = _foundBoundingBox
            };
        }

        public ICropAnalysis GetAnalysis()
        {
            return _cropAnalysis;
        }

        private bool ValidateRectangle(Rectangle rectangle)
        {
            if (rectangle == null) return false;
            if (rectangle.Width < 3) return false;
            if (rectangle.Height < 3)  return false;

            return true;
        }

        /*
         Bounding box example
        */
        private unsafe Rectangle GetBoundingBoxForContentRgb(BitmapData bitmap, Rectangle rectangle, Color backgroundColor, int threshold)
        {
            // Bytes per pixel
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var w = rectangle.Right;
            var h = rectangle.Bottom;

            // Stride, scan width.
            var s = bitmap.Stride;

            // Scan0, pointer to first scan.
            var s0 = (byte*)bitmap.Scan0;

            // Calculated x-min
            var xn = w;

            // Calculated x-max
            var xm = rectangle.X;

            // Calculated y-min
            var yn = h;

            // Calculated y-max
            var ym = rectangle.Y;

            for (var y = rectangle.Y; y < h; y++)
            {
                // Pointer to current scanline
                var row = s0 + y * s;

                for (var x = rectangle.X; x < w; x++)
                {
                    // Pointer to current pixel
                    var p = x * bpp;

                    // Pixels are stored in b,g,r-order
                    // In this case one byte per color
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    // Delta color values
                    var bd = Math.Abs(b - backgroundColor.B);
                    var gd = Math.Abs(g - backgroundColor.G);
                    var rd = Math.Abs(r - backgroundColor.R);

                    // Grayscale operation on color delta
                    // This is done to properly evaluate if 
                    // the perceptual differences are above threshold

                    if (0.299 * rd + 0.587 * gd + 0.114 * bd <= threshold)
                        continue;

                    if (x < xn) xn = x;
                    if (x > xm) xm = x;
                    if (y < yn) yn = y;
                    if (y > ym) ym = y;
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
                        continue;

                    if (x < xn) xn = x;
                    if (x > xm) xm = x;
                    if (y < yn) yn = y;
                    if (y > ym) ym = y;
                }
            }

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }
    }
}

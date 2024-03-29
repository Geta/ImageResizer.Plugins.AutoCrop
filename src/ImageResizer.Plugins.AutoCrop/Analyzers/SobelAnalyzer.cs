﻿using ImageResizer.Plugins.AutoCrop.Actions;
using ImageResizer.Plugins.AutoCrop.Detection;
using ImageResizer.Plugins.AutoCrop.Extensions;
using ImageResizer.Plugins.AutoCrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class SobelAnalyzer : ICropAnalyzer
    {
        private const int _thumbnailSize = 256;

        private readonly bool _foundBoundingBox;
        private readonly Rectangle _boundingBox;
        private readonly ICropAnalysis _analysis;

        public SobelAnalyzer(BitmapData bitmap, int sobelThreshold, float bucketTreshold)
        {
            var thumbnail = GetThumbnail(bitmap);
            var thumbnailData = thumbnail.LockBits(new Rectangle(0, 0, thumbnail.Width, thumbnail.Height), ImageLockMode.ReadOnly, thumbnail.PixelFormat);
            var thumbnailInspection = new BorderInspector(thumbnailData, _thumbnailSize, 1.0f);

            thumbnail.UnlockBits(thumbnailData);

            var features = GetFeatureMap(thumbnail);
            var featureBox = new Rectangle(0, 0, features.Width, features.Height);
            var featureData = features.LockBits(featureBox, ImageLockMode.ReadOnly, features.PixelFormat);
            var featureInspection = new BorderInspector(featureData, sobelThreshold, bucketTreshold, forceRgb: true);
            
            if (featureInspection.Failed)
            {
                featureBox = featureBox.Contract(10);

                var additionalInspection = new BorderInspector(featureData, featureBox, sobelThreshold, bucketTreshold);
                if (!additionalInspection.Failed)
                {
                    featureInspection = additionalInspection;
                }
            }

            if (featureInspection.Failed)
            {
                _boundingBox = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                _foundBoundingBox = false;
            }
            else
            {
                _boundingBox = GetBoundingBoxForFeature(featureData, featureInspection.Rectangle, sobelThreshold, bitmap.Width, bitmap.Height);
                _foundBoundingBox = true;
            }
            
            features.UnlockBits(featureData);

            features.Dispose();
            thumbnail.Dispose();

            _analysis = new CropAnalysis
            {
                Success = _foundBoundingBox,
                BoundingBox = _boundingBox,
                Background = thumbnailInspection.BackgroundColor,
            };
        }        

        public ICropAnalysis GetAnalysis()
        {
            return _analysis;
        }

        private Bitmap GetThumbnail(BitmapData data)
        {
            return Raw.Approximate(data, _thumbnailSize, _thumbnailSize);
        }

        private Bitmap GetFeatureMap(Bitmap thumbnail)
        {
            return Filter.Sobel(thumbnail);
        }

        private unsafe Rectangle GetBoundingBoxForFeature(BitmapData bitmap, Rectangle rectangle, int threshold, int width, int height)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = rectangle.Bottom;
            var w = rectangle.Right;

            var hr = height / (float)h;
            var wr = width / (float)w;

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

                        if (0.299 * r + 0.587 * g + 0.114 * b <= threshold)
                            continue;

                        if (x < xn) xn = x;
                        if (x > xm) xm = x;
                        if (y < yn) yn = y;
                        if (y > ym) ym = y;
                    }
                }
            }

            return new Rectangle((int)Math.Floor(xn * wr), (int)Math.Floor(yn * hr), (int)Math.Ceiling((xm - xn) * wr), (int)Math.Ceiling((ym - yn) * hr));
        }
    }
}

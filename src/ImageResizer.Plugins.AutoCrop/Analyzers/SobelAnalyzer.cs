using ImageResizer.Plugins.AutoCrop.Actions;
using ImageResizer.Plugins.AutoCrop.Detection;
using ImageResizer.Plugins.AutoCrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class SobelAnalyzer : IAnalyzer
    {
        private readonly IAnalysis _analysis;
        private const int _featureSize = 256;

        public SobelAnalyzer(BitmapData bitmap, int threshold)
        {
            var features = GetFeatureMap(bitmap);
            var featureData = features.LockBits(new Rectangle(0, 0, features.Width, features.Height), ImageLockMode.ReadOnly, features.PixelFormat);
            var bounds = GetBoundingBoxForFeature(featureData, threshold, bitmap.Width, bitmap.Height);

            features.UnlockBits(featureData);

            var errorX = bitmap.Width / (float)_featureSize;
            var errorY = bitmap.Height / (float)_featureSize;

            var borderInspection = new BorderInspector(bitmap, bounds, threshold, 0.95f);

            var success = bounds.X > errorX || 
                          bounds.Y > errorY || 
                          bounds.Width < bitmap.Width - errorX || 
                          bounds.Height < bitmap.Height - errorY;

            _analysis = new ImageAnalysis
            {
                BoundingBox = bounds,
                Background = borderInspection.BackgroundColor,
                Success = success
            };
        }        

        public IAnalysis GetAnalysis()
        {
            return _analysis;
        }

        private Bitmap GetFeatureMap(BitmapData data)
        {
            var w = Math.Min(data.Width, _featureSize);
            var h = Math.Min(data.Height, _featureSize);

            var approximate = Raw.Approximate(data, w, h);

            return Filter.Sobel(approximate);
        }

        private unsafe Rectangle GetBoundingBoxForFeature(BitmapData bitmap, int threshold, int width, int height)
        {
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            var h = bitmap.Height;
            var w = bitmap.Width;

            var hr = height / (float)h;
            var wr = width / (float)w;

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

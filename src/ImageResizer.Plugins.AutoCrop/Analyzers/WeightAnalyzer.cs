using ImageResizer.Plugins.AutoCrop.Detection;
using ImageResizer.Plugins.AutoCrop.Models;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class WeightAnalyzer : IWeightAnalyzer
    {
        private readonly IWeightAnalysis _analysis;

        private readonly int _mapResolution;

        public WeightAnalyzer(Bitmap bitmap, Color background)
        {
            _mapResolution = 5;

            var weightMap = GetWeightMap(bitmap);
            var bounds = new Rectangle(0, 0, _mapResolution, _mapResolution);
            var bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            var data = (BitmapData)null;
            var vector = new PointF(0, 0);

            try
            {
                data = weightMap.LockBits(bounds, ImageLockMode.ReadOnly, bitmap.PixelFormat);

                if (bytesPerPixel == 3)
                {
                    vector = GetVectorRgb(data, background);
                }
                else
                {
                    vector = GetVectorRgba(data, background);
                }
            }
            finally
            {
                if (weightMap != null)
                {
                    if (data != null)
                        weightMap.UnlockBits(data);

                    weightMap.Dispose();
                }                    
            }

            _analysis = new WeightAnalysis
            {
                Weight = vector
            };
        }

        public IWeightAnalysis GetAnalysis()
        {
            return _analysis;
        }

        private Bitmap GetWeightMap(Bitmap source)
        {
            var settings = new Instructions
            {
                Width = _mapResolution,
                Height = _mapResolution,
                Mode = FitMode.Stretch,
                Scale = ScaleMode.Both
            };

            var job = new ImageJob(source, typeof(Bitmap), settings, false, false);

            ImageBuilder.Current.Build(job);

            return job.Result as Bitmap;
        }

        private unsafe PointF GetVectorRgb(BitmapData bitmap, Color backgroundColor)
        {
            // Bytes per pixel
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            var offset = _mapResolution / 2.0;
            var average = 1 / (double)(_mapResolution * _mapResolution);

            var w = bitmap.Width;
            var h = bitmap.Height;

            // Stride, scan width.
            var s = bitmap.Stride;

            // Scan0, pointer to first scan.
            var s0 = (byte*)bitmap.Scan0;

            var weight = new PointF(0, 0);

            for (var y = 0; y < h; y++)
            {
                // Normalized vector position
                var yn = (y - offset) / offset;

                // Pointer to current scanline
                var row = s0 + y * s;

                for (var x = 0; x < w; x++)
                {
                    // Normalized vector position
                    var xn = (x - offset) / offset;

                    // Pointer to current pixel
                    var p = x * bpp;

                    // Pixels are stored in b,g,r-order
                    // In this case one byte per color
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    // Delta color values
                    var bd = Math.Abs(b - backgroundColor.B) * Constants.BytePrecision;
                    var gd = Math.Abs(g - backgroundColor.G) * Constants.BytePrecision;
                    var rd = Math.Abs(r - backgroundColor.R) * Constants.BytePrecision;

                    var d = 0.299 * rd + 0.587 * gd + 0.114 * bd;
                    var v = new PointF((float)(xn * d * average),(float)(yn * d * average));

                    weight = new PointF(weight.X + v.X, weight.Y + v.Y);
                }
            }

            return weight;
        }

        private unsafe PointF GetVectorRgba(BitmapData bitmap, Color backgroundColor)
        {
            // Bytes per pixel
            var bpp = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            var offset = _mapResolution / 2;
            var average = 1 / (double)(_mapResolution * _mapResolution);

            var w = bitmap.Width;
            var h = bitmap.Height;

            // Stride, scan width.
            var s = bitmap.Stride;

            // Scan0, pointer to first scan.
            var s0 = (byte*)bitmap.Scan0;

            var weight = new PointF(0, 0);

            for (var y = 0; y < h; y++)
            {
                // Normalized vector position
                var yn = y - offset;

                // Pointer to current scanline
                var row = s0 + y * s;

                for (var x = 0; x < w; x++)
                {
                    // Normalized vector position
                    var xn = x - offset;

                    // Pointer to current pixel
                    var p = x * bpp;

                    // Pixels are stored in b,g,r-order
                    // In this case one byte per color
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    // Delta color values
                    var bd = Math.Abs(b - backgroundColor.B) * Constants.BytePrecision;
                    var gd = Math.Abs(g - backgroundColor.G) * Constants.BytePrecision;
                    var rd = Math.Abs(r - backgroundColor.R) * Constants.BytePrecision;

                    var d = (0.299 * rd + 0.587 * gd + 0.114 * bd) * a * Constants.BytePrecision;
                    var v = new PointF((float)(xn * d * average), (float)(yn * d * average));

                    weight = new PointF(weight.X + v.X, weight.Y + v.Y);
                }
            }

            return weight;
        }
    }
}

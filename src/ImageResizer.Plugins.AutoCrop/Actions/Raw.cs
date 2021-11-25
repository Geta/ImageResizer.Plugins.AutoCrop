using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Actions
{
    public static class Raw
    {
        public static unsafe void Copy(Bitmap source, Rectangle from, Bitmap target, Point to)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source.PixelFormat != target.PixelFormat) throw new ArgumentException("PixelFormats must match");

            var pixelFormat = source.PixelFormat;
            var bpp = Image.GetPixelFormatSize(pixelFormat) / 8;

            var sourceData = source.LockBits(from, ImageLockMode.ReadOnly, pixelFormat);

            try
            {
                var targetData = target.LockBits(new Rectangle(to, from.Size), ImageLockMode.WriteOnly, pixelFormat);

                try
                {
                    var ss0 = (byte*)sourceData.Scan0;
                    var ts0 = (byte*)targetData.Scan0;
                    var ss = sourceData.Stride;
                    var ts = targetData.Stride;

                    for (var y = 0; y < from.Height; y++)
                    {
                        var srow = ss0 + y * ss;
                        var trow = ts0 + y * ts;

                        for (var x = 0; x < from.Width; x++)
                        {
                            for (var p = 0; p < bpp; p++)
                            {
                                var c = x * bpp + p;

                                trow[c] = srow[c];
                            }
                        }
                    }
                }
                finally
                {
                    target.UnlockBits(targetData);
                }
            }
            finally
            {
                source.UnlockBits(sourceData);
            }
        }

        public static unsafe void FillAlpha(Bitmap target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var data = target.LockBits(new Rectangle(0, 0, target.Width, target.Height), ImageLockMode.WriteOnly, target.PixelFormat);

            try
            {
                FillAlpha(data);
            }
            finally
            {
                target.UnlockBits(data);
            }            
        }

        public static unsafe void FillAlpha(BitmapData target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var bpp = Image.GetPixelFormatSize(target.PixelFormat) / 8;
            if (bpp < 4)
                return;

            var w = target.Width;
            var h = target.Height;
            
            var s0 = (byte*)target.Scan0;
            var s = target.Stride;

            for (var y = 0; y < h; y++)
            {
                var row = s0 + y * s;

                for (var x = 0; x < w; x++)
                {
                    row[x * bpp + 3] = byte.MaxValue;
                }
            }
        }

        public static unsafe void FillRgb(Bitmap target, Color color)
        {            
            if (target == null) 
                throw new ArgumentNullException(nameof(target));

            // Bytes per pixel
            var bpp = Image.GetPixelFormatSize(target.PixelFormat) / 8;

            var w = target.Width;
            var h = target.Height;

            // Lock memory address of image
            var data = target.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, target.PixelFormat);
            
            try
            {
                // Stride, scan width.
                var s = data.Stride;

                // Scan0, pointer to first scan.
                var s0 = (byte*)data.Scan0;

                for (var y = 0; y < h; y++)
                {
                    // Pointer to current scanline
                    var row = s0 + y * s;

                    for (var x = 0; x < w; x++)
                    {
                        // Pointer to current pixel
                        var p = x * bpp;

                        // Pixels are stored in b,g,r-order
                        // In this case one byte per color
                        row[p] = color.B;
                        row[p + 1] = color.G;
                        row[p + 2] = color.R;
                    }
                }
            }
            finally
            {
                target.UnlockBits(data);
            }
        }

        public static unsafe void FillRgba(Bitmap target, Color color)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var w = target.Width;
            var h = target.Height;
            var bpp = Image.GetPixelFormatSize(target.PixelFormat) / 8;
            var data = target.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, target.PixelFormat);
            
            try
            {
                var s0 = (byte*)data.Scan0;
                var s = data.Stride;

                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;

                    for (var x = 0; x < w; x++)
                    {
                        var p = x * bpp;

                        // Pixels are stored in b,g,r,a-order
                        // In this case one byte per color
                        row[p] = color.B;
                        row[p + 1] = color.G;
                        row[p + 2] = color.R;
                        row[p + 3] = color.A;
                    }
                }       
            }
            finally
            {
                target.UnlockBits(data);
            }
        }

        public static unsafe Bitmap Approximate(Bitmap source, int width, int height)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var sourceData = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, source.PixelFormat);

            try
            {
                var target = Approximate(sourceData, width, height);
                return target;
            }
            finally
            {
                source.UnlockBits(sourceData);
            }
        }

        public static unsafe Bitmap Approximate(BitmapData sourceData, int width, int height)
        {
            if (sourceData == null) throw new ArgumentNullException(nameof(sourceData));

            if (width > sourceData.Width)
                width = sourceData.Width;

            if (height > sourceData.Height)
                height = sourceData.Height;

            var pixelFormat = sourceData.PixelFormat;
            var bpp = Image.GetPixelFormatSize(pixelFormat) / 8;

            var target = new Bitmap(width, height, pixelFormat);
            var targetData = target.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);

            try
            {
                var ss0 = (byte*)sourceData.Scan0;
                var ts0 = (byte*)targetData.Scan0;

                var ss = sourceData.Stride;
                var ts = targetData.Stride;

                var stepX = sourceData.Width / (float)width;
                var stepY = sourceData.Height / (float)height;

                for (var y = 0; y < height; y++)
                {
                    var srow = ss0 + (int)Math.Floor(y * stepY) * ss;
                    var trow = ts0 + y * ts;

                    for (var x = 0; x < width; x++)
                    {
                        for (var p = 0; p < bpp; p++)
                        {
                            var sc = (int)Math.Floor(x * stepX) * bpp + p;
                            var tc = x * bpp + p;

                            trow[tc] = srow[sc];
                        }
                    }
                }
            }
            finally
            {
                target.UnlockBits(targetData);
            }
            
            return target;
        }
    }
}

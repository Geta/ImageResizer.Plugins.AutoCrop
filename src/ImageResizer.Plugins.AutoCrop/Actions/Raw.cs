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
            var targetData = target.LockBits(new Rectangle(to, from.Size), ImageLockMode.WriteOnly, pixelFormat);

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

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);
        }

        public static unsafe void FillRgb(Bitmap target, Color color)
        {            
            if (target == null) throw new ArgumentNullException(nameof(target));

            var w = target.Width;
            var h = target.Height;
            var bpp = Image.GetPixelFormatSize(target.PixelFormat) / 8;
            var data = target.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, target.PixelFormat);
            var s0 = (byte*)data.Scan0;
            var s = data.Stride;

            for (var y = 0; y < h; y++)
            {
                var row = s0 + y * s;

                for (var x = 0; x < w; x++)
                {
                    var p = x * bpp;

                    row[p] = color.B;
                    row[p + 1] = color.G;
                    row[p + 2] = color.R;
                }
            }

            target.UnlockBits(data);
        }

        public static unsafe void FillRgba(Bitmap target, Color color)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));

            var w = target.Width;
            var h = target.Height;
            var bpp = Image.GetPixelFormatSize(target.PixelFormat) / 8;
            var data = target.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, target.PixelFormat);
            var s0 = (byte*)data.Scan0;
            var s = data.Stride;

            for (var y = 0; y < h; y++)
            {
                var row = s0 + y * s;

                for (var x = 0; x < w; x++)
                {
                    var p = x * bpp;

                    row[p] = color.B;
                    row[p + 1] = color.G;
                    row[p + 2] = color.R;
                    row[p + 3] = color.A;
                }
            }

            target.UnlockBits(data);
        }
    }
}

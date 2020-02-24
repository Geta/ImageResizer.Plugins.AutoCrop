using ImageResizer.Plugins.AutoCrop.Extensions;
using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Actions
{
    public static class Filter
    {
        public static unsafe Bitmap Sobel(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = (byte)Image.GetPixelFormatSize(pixelFormat) / 8;
            var hasAlpha = bpp > 3;

            if (hasAlpha)
            {
                source = GrayscaleRgba(source);
            }
            else
            {
                source = GrayscaleRgb(source);
            }

            var target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var imageBox = new Rectangle(0, 0, source.Width, source.Height);

            var sourceData = source.LockBits(imageBox, ImageLockMode.ReadOnly, pixelFormat);
            var targetData = target.LockBits(imageBox, ImageLockMode.WriteOnly, pixelFormat);

            var s0 = (byte*)sourceData.Scan0;
            var t0 = (byte*)targetData.Scan0;
            var s = sourceData.Stride;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var yn = Math.Max(0, y - 1);
                    var ym = Math.Min(source.Height - 1, y + 1);
                    
                    var ybn = yn * bpp;
                    var yb = y * bpp;
                    var ybm = ym * bpp;

                    var rn = s0 + yn * s;
                    var r = s0 + y * s;
                    var rm = s0 + ym * s;
                    
                    var tr = t0 + y * s;

                    for (var x = 0; x < source.Width; x++)
                    {   
                        var xn = Math.Max(0, x - 1);
                        var xm = Math.Min(source.Width - 1, x + 1);

                        var xbn = xn * bpp;
                        var xb = x * bpp;
                        var xbm = xm * bpp;

                        var dx = rn[xbn] * -1 + rn[xbm] + r[xbn] * -2 + r[xbm] * 2 + rm[xbn] * -1 + rm[xbm];
                        var dy = rn[xbn] + rn[xb] * 2 + rn[xbm] + rm[xbn] * -1 + rm[xb] * -2 + rm[xbm] * -1;

                        var mag = Math.Sqrt((dx * dx) + (dy * dy));
                        if (mag > byte.MaxValue)
                        {
                            tr[xb] = byte.MaxValue;
                            tr[xb + 1] = byte.MaxValue;
                            tr[xb + 2] = byte.MaxValue;
                        }
                        else if (mag < 0)
                        {
                            tr[xb] = 0;
                            tr[xb + 1] = 0;
                            tr[xb + 2] = 0;
                        }
                        else
                        {
                            var c = (byte)mag;

                            tr[xb] = c;
                            tr[xb + 1] = c;
                            tr[xb + 2] = c;
                        }
                    }
                }
            }

            if (bpp > 3)
            {
                Raw.FillAlpha(targetData);
            }

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

            return target;
        }

        public static unsafe Bitmap Buckets(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = (byte)Image.GetPixelFormatSize(pixelFormat) / 8;
            var hasAlpha = bpp > 3;

            var target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var imageBox = new Rectangle(0, 0, source.Width, source.Height);

            var sourceData = source.LockBits(imageBox, ImageLockMode.ReadOnly, pixelFormat);
            var targetData = target.LockBits(imageBox, ImageLockMode.WriteOnly, pixelFormat);

            var s0 = (byte*)sourceData.Scan0;
            var t0 = (byte*)targetData.Scan0;
            var s = sourceData.Stride;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var srow = s0 + y * s;
                    var trow = t0 + y * s;

                    for (var x = 0; x < source.Width; x++)
                    {
                        var p = x * bpp;
                        var b = srow[p];
                        var g = srow[p + 1];
                        var r = srow[p + 2];
                        var a = hasAlpha ? srow[p + 3] : byte.MaxValue;

                        var v = Color.FromArgb(a, r, g, b).ToColorBucket();
                        var c = v.ToColorValue();

                        trow[p] = c;
                        trow[p + 1] = c;
                        trow[p + 2] = c;
                    }
                }
            }

            if (bpp > 3)
            {
                Raw.FillAlpha(targetData);
            }

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

            return target;
        }

        public static unsafe Bitmap GrayscaleRgb(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = (byte)Image.GetPixelFormatSize(pixelFormat) / 8;
            var hasAlpha = bpp > 3;

            var target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var imageBox = new Rectangle(0, 0, source.Width, source.Height);

            var sourceData = source.LockBits(imageBox, ImageLockMode.ReadOnly, pixelFormat);
            var targetData = target.LockBits(imageBox, ImageLockMode.WriteOnly, pixelFormat);

            var s0 = (byte*)sourceData.Scan0;
            var t0 = (byte*)targetData.Scan0;
            var s = sourceData.Stride;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var srow = s0 + y * s;
                    var trow = t0 + y * s;

                    for (var x = 0; x < source.Width; x++)
                    {
                        var p = x * bpp;
                        var v = (byte)(0.299 * srow[p + 2] + 0.587 * srow[p + 1] + 0.114 * srow[p]);

                        trow[p] = v;
                        trow[p + 1] = v;
                        trow[p + 2] = v;
                    }
                }
            }

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

            return target;
        }

        public static unsafe Bitmap GrayscaleRgba(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = (byte)Image.GetPixelFormatSize(pixelFormat) / 8;
            var hasAlpha = bpp > 3;

            var target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var imageBox = new Rectangle(0, 0, source.Width, source.Height);

            var sourceData = source.LockBits(imageBox, ImageLockMode.ReadOnly, pixelFormat);
            var targetData = target.LockBits(imageBox, ImageLockMode.WriteOnly, pixelFormat);

            var s0 = (byte*)sourceData.Scan0;
            var t0 = (byte*)targetData.Scan0;
            var s = sourceData.Stride;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var srow = s0 + y * s;
                    var trow = t0 + y * s;

                    for (var x = 0; x < source.Width; x++)
                    {
                        var p = x * bpp;
                        var v = (byte)(0.299 * srow[p + 2] + 0.587 * srow[p + 1] + 0.114 * srow[p]);
                        var c = (byte)Math.Min(byte.MaxValue, v * (srow[p + 3] * Constants.BytePrecision) + (byte.MaxValue - srow[p + 3]));

                        trow[p] = c;
                        trow[p + 1] = c;
                        trow[p + 2] = c;
                        trow[p + 3] = byte.MaxValue;
                    }
                }
            }

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

            return target;
        }
    }
}

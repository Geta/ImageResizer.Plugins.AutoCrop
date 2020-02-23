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
            var target = new Bitmap(source.Width, source.Height, source.PixelFormat);
            var imageBox = new Rectangle(0, 0, source.Width, source.Height);

            var sourceData = source.LockBits(imageBox, ImageLockMode.ReadOnly, pixelFormat);
            var targetData = target.LockBits(imageBox, ImageLockMode.WriteOnly, pixelFormat);

            var s0 = (byte*)sourceData.Scan0;
            var t0 = (byte*)targetData.Scan0;
            var s = sourceData.Stride;

            var hasAlpha = bpp > 3;

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

                        // All color information is baked as grayscale, luminance is enough to find edges.
                        var c11 = (byte)(0.299 * rn[xbn + 2] + 0.587 * rn[xbn + 1] + 0.114 * rn[xbn]);
                        var c12 = (byte)(0.299 * rn[xb + 2] + 0.587 * rn[xb + 1] + 0.114 * rn[xb]);
                        var c13 = (byte)(0.299 * rn[xbm + 2] + 0.587 * rn[xbm + 1] + 0.114 * rn[xbm]);

                        var c21 = (byte)(0.299 * r[xbn + 2] + 0.587 * r[xbn + 1] + 0.114 * r[xbn]);
                        //var c22 = (byte)(0.299 * r[xb + 2] + 0.587 * r[xb + 1] + 0.114 * r[xb]);
                        var c23 = (byte)(0.299 * rn[xbm + 2] + 0.587 * rn[xbm + 1] + 0.114 * rn[xbm]);

                        var c31 = (byte)(0.299 * rm[xbn + 2] + 0.587 * rm[xbn + 1] + 0.114 * rm[xbn]);
                        var c32 = (byte)(0.299 * rm[xb + 2] + 0.587 * rm[xb + 1] + 0.114 * rm[xb]);
                        var c33 = (byte)(0.299 * rm[xb + 2] + 0.587 * rm[xb + 1] + 0.114 * rm[xb]);

                        // Since many images define entirely transparent color as black,
                        // There might be a rather harsh shift between totally transparent and slightly visible colors.
                        // To avoid this invisible edge, all alpha information should be added as white shift of existing colors.
                        if (hasAlpha)
                        {
                            c11 = (byte)Math.Min(byte.MaxValue, c11 * (rn[xbn + 3] * Constants.BytePrecision) + (byte.MaxValue - rn[xbn + 3]));
                            c12 = (byte)Math.Min(byte.MaxValue, c12 * (rn[xb + 3] * Constants.BytePrecision) + (byte.MaxValue - rn[xb + 3]));
                            c13 = (byte)Math.Min(byte.MaxValue, c13 * (rn[xbm + 3] * Constants.BytePrecision) + (byte.MaxValue - rn[xbm + 3])); 

                            c21 = (byte)Math.Min(byte.MaxValue, c21 * (r[xbn + 3] * Constants.BytePrecision) + (byte.MaxValue - r[xbn + 3]));
                            //c22 = (byte)Math.Min(byte.MaxValue, c22 * (r[xb + 3] * Constants.BytePrecision) + (byte.MaxValue - r[xb + 3]));
                            c23 = (byte)Math.Min(byte.MaxValue, c23 * (r[xbm + 3] * Constants.BytePrecision) + (byte.MaxValue - r[xbm + 3])); 

                            c31 = (byte)Math.Min(byte.MaxValue, c31 * (rm[xbn + 3] * Constants.BytePrecision) + (byte.MaxValue - rm[xbn + 3]));
                            c32 = (byte)Math.Min(byte.MaxValue, c32 * (rm[xb + 3] * Constants.BytePrecision) + (byte.MaxValue - rm[xb + 3])); 
                            c33 = (byte)Math.Min(byte.MaxValue, c33 * (rm[xbm + 3] * Constants.BytePrecision) + (byte.MaxValue - rm[xbm + 3])); 
                        }

                        // This is just two optimized sobel kernels (all 0 multiplications removed).
                        var dx = c11 * -1 + c13 + c21 * -2 + c23 * 2 + c31 * -1 + c33;
                        var dy = c11 + c12 * 2 + c13 + c31 * -1 + c32 * -2 + c33 * -1;

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

                        if (hasAlpha)
                        {
                            tr[xb + 3] = byte.MaxValue;
                        }
                    }
                }
            }

            source.UnlockBits(sourceData);
            target.UnlockBits(targetData);

            return target;
        }

        public static unsafe void Buckets(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = Image.GetPixelFormatSize(pixelFormat) / 8;
            var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadWrite, pixelFormat);

            var s0 = (byte*)data.Scan0;
            var s = data.Stride;

            var hasAlpha = bpp > 3;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var row = s0 + y * s;

                    for (var x = 0; x < source.Width; x++)
                    {
                        var p = x * bpp;
                        var b = row[p];
                        var g = row[p + 1];
                        var r = row[p + 2];
                        var a = hasAlpha ? row[p + 3] : byte.MaxValue;

                        var v = Color.FromArgb(a, r, g, b).ToColorBucket();
                        var c = v.ToColorValue();

                        row[p] = c;
                        row[p + 1] = c;
                        row[p + 2] = c;
                    }
                }
            }

            if (bpp > 3)
            {
                Raw.FillAlpha(data);
            }

            source.UnlockBits(data);
        }

        public static unsafe void Grayscale(Bitmap source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var pixelFormat = source.PixelFormat;
            var bpp = Image.GetPixelFormatSize(pixelFormat) / 8; 
            var data = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, pixelFormat);

            var s0 = (byte*)data.Scan0;
            var s = data.Stride;

            unchecked
            {
                for (var y = 0; y < source.Height; y++)
                {
                    var row = s0 + y * s;

                    for (var x = 0; x < source.Height; x++)
                    {
                        var p = x * bpp;
                        var v = (byte)(0.299 * row[p + 2] + 0.587 * row[p + 1] + 0.114 * row[p]);

                        row[p] = row[p + 1] = row[p + 2] = v;
                    }
                }
            }

            source.UnlockBits(data);
        }
    }
}

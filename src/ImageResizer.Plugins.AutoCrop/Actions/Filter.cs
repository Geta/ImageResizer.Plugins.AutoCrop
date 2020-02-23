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
                        var xbm = x * bpp;

                        for (var p = 0; p < 3; p++)
                        {
                            var c11 = rn[xbn + p];
                            var c12 = rn[xb + p];
                            var c13 = rn[xbm + p];

                            var c21 = r[xbn + p];
                            //var c22 = r[xb + p];
                            var c23 = r[xbm + p];

                            var c31 = rm[xbn + p];
                            var c32 = rm[xb + p];
                            var c33 = rm[xbm + p];

                            /* Unoptimized calculation
                            var dx = c11 * -1 + c12 * 0 + c13 * 1
                                   + c21 * -2 + c22 * 0 + c23 * 2
                                   + c31 * -1 + c32 * 0 + c33 * 1;

                            var dy = c11 * 1 + c12 * 2 + c13 * 1
                                   + c21 * 0 + c22 * 0 + c23 * 0
                                   + c31 * -1 + c32 * -2 + c33 * -1;
                            */

                            /* Optimized number of calculations */
                            var dx = c11 * -1 + c13 + c21 * -2 + c23 * 2 + c31 * -1 + c33;
                            var dy = c11 + c12 * 2 + c13 + c31 * -1 + c32 * -2 + c33 * -1;

                            var mag = Math.Sqrt((dx * dx) + (dy * dy));
                        
                            if (mag > byte.MaxValue)
                            {
                                tr[xb + p] = byte.MaxValue;
                            }
                            else if (mag < 0)
                            {
                                tr[xb + p] = 0;
                            }
                            else
                            {
                                tr[xb + p] = (byte)mag;
                            }
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

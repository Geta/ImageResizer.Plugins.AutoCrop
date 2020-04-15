using ImageResizer.Plugins.AutoCrop.Extensions;
using ImageResizer.Plugins.AutoCrop.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Detection
{
    public class BorderInspector
    {
        public readonly bool Failed;
        public readonly int BytesPerPixel;
        public readonly Color BackgroundColor;
        public readonly float BucketRatio;
        public readonly Rectangle Rectangle;

        public BorderInspector(BitmapData bitmap, Rectangle rectangle, int colorThreshold, float bucketThreshold, bool forceRgb = false)
        {
            var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            if (!bounds.Contains(rectangle))
                throw new ArgumentException("Rectangle must be inside image bounds", nameof(rectangle));

            BytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            Rectangle = rectangle;

            BorderAnalysis result;

            if (forceRgb)
            {
                result = AnalyzeRgb(bitmap, rectangle, colorThreshold, bucketThreshold);
            }
            else
            {
                switch (BytesPerPixel)
                {
                    case 4: result = AnalyzeArgb(bitmap, rectangle, colorThreshold, bucketThreshold); break;
                    case 3: result = AnalyzeRgb(bitmap, rectangle, colorThreshold, bucketThreshold); break;
                    default: result = new BorderAnalysis(); break;
                }
            }            

            Failed = !result.Success;
            BackgroundColor = result.Background;
            BucketRatio = result.BucketRatio;
        }

        public BorderInspector(BitmapData bitmap, int colorThreshold, float bucketThreshold, bool forceRgb = false) : 
            this(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), colorThreshold, bucketThreshold, forceRgb)
        { 
        
        }

        private unsafe BorderAnalysis AnalyzeRgb(BitmapData bitmap, Rectangle rectangle, int colorThreshold, float bucketThreshold)
        {
            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var colors = new Dictionary<Color, int>(colorThreshold);
            var buckets = new Dictionary<int, int>(ColorExtensions.GetMaxColorBuckets());

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = rectangle.Left * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }                    
                }

                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = (rectangle.Right - 1) * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + rectangle.Top * s;
                    
                    var p = x * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + (rectangle.Bottom - 1) * s;
                    
                    var p = x * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }
            }
            
            return new BorderAnalysis(colors, buckets, colorThreshold, bucketThreshold);
        }

        private unsafe BorderAnalysis AnalyzeArgb(BitmapData bitmap, Rectangle rectangle, int colorThreshold, float bucketThreshold)
        {
            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var colors = new Dictionary<Color, int>(colorThreshold);
            var buckets = new Dictionary<int, int>(ColorExtensions.GetMaxColorBuckets());

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;

                    var p = rectangle.Left * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];
                    
                    var c = a == 0 ? Color.Transparent : Color.FromArgb(a, r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }

                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = (rectangle.Right - 1) * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = a == 0 ? Color.Transparent : Color.FromArgb(a, r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + rectangle.Top * s;

                    var p = x * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = a == 0 ? Color.Transparent : Color.FromArgb(a, r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + (rectangle.Bottom - 1) * s;

                    var p = x * BytesPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = a == 0 ? Color.Transparent : Color.FromArgb(a, r, g, b);
                    var ce = colors.ContainsKey(c);

                    if (ce)
                    {
                        colors[c]++;
                    }
                    else if (colors.Count < colorThreshold)
                    {
                        colors.Add(c, 1);
                    }

                    if (ce || colors.Count >= colorThreshold)
                    {
                        var cb = c.ToColorBucket();
                        if (buckets.ContainsKey(cb))
                        {
                            buckets[cb]++;
                        }
                        else
                        {
                            buckets.Add(cb, 1);
                        }
                    }
                }
            }

            return new BorderAnalysis(colors, buckets, colorThreshold, bucketThreshold);
        }
    }
}
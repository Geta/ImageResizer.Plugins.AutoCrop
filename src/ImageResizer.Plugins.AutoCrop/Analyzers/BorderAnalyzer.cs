using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer.Plugins.AutoCrop.Models;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public class BorderAnalyzer
    {
        public readonly bool BorderIsDirty;
        public readonly int BitsPerPixel;
        public readonly Color BackgroundColor;

        public BorderAnalyzer(BitmapData bitmap, int threshold)
        {
            BitsPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

            BorderAnalysisResult result;

            switch (BitsPerPixel)
            {
                case 4: result = AnalyzeArgb(bitmap, threshold); break;
                case 3: result = AnalyzeRgb(bitmap, threshold); break;
                default: result = new BorderAnalysisResult(); break;
            }

            BorderIsDirty = !result.Success;
            BackgroundColor = result.Background;
        }

        private unsafe BorderAnalysisResult AnalyzeRgb(BitmapData bitmap, int threshold)
        {
            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var colors = new Dictionary<Color, int>();

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = 0 * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = w * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + 0 * s;
                    
                    var p = x * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb(r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + (h - 1) * s;
                    
                    var p = x * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];

                    var c = Color.FromArgb( r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }
            }
            
            return new BorderAnalysisResult(colors, threshold);
        }

        private unsafe BorderAnalysisResult AnalyzeArgb(BitmapData bitmap, int threshold)
        {
            var h = bitmap.Height;
            var w = bitmap.Width;
            var s = bitmap.Stride;
            var s0 = (byte*)bitmap.Scan0;

            var colors = new Dictionary<Color, int>();

            unchecked
            {
                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = 0 * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = Color.FromArgb(a, r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var y = 0; y < h; y++)
                {
                    var row = s0 + y * s;
                    
                    var p = w * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = Color.FromArgb(a, r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + 0 * s;
                    
                    var p = x * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = Color.FromArgb(a, r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }

                for (var x = 0; x < w; x++)
                {
                    var row = s0 + (h - 1) * s;
                    
                    var p = x * BitsPerPixel;
                    var b = row[p];
                    var g = row[p + 1];
                    var r = row[p + 2];
                    var a = row[p + 3];

                    var c = Color.FromArgb(a, r, g, b);

                    if (colors.ContainsKey(c))
                    {
                        colors[c]++;
                    }
                    else
                    {
                        colors.Add(c, 1);
                    }
                }
            }
            
            return new BorderAnalysisResult(colors, threshold);
        }
    }
}
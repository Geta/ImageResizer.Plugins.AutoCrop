using System;
using System.Collections.Generic;
using ImageResizer.Configuration;
using ImageResizer.Resizing;
using AutoCrop.Core.Analyzers;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Plugins.AutoPad.Models;

namespace ImageResizer.Plugins.AutoPad
{
    public class AutoPadPlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[]
            {
                "autoPad"
            };
        }

        public readonly string DataKey = "autopad";

        protected override RequestedAction PostPrepareSourceBitmap(ImageState state)
        {
            if (state == null) return RequestedAction.None;
            if (state.settings == null) return RequestedAction.None;
            if (state.sourceBitmap == null) return RequestedAction.None;

            var bitmap = state.sourceBitmap;
            if (!IsRequiredSize(bitmap)) return RequestedAction.None;

            var pixelFormat = bitmap.PixelFormat;
            if (!IsCorrectFormat(pixelFormat)) return RequestedAction.None;

            var setting = state.settings["autoPad"];
            if (setting == null) return RequestedAction.None;

            var settings = ParseSettings(setting);

            try
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);                
                var cropAnalyzer = new BorderAnalyzer(data, 35);
                var analyzer = new BorderAnalyzer(data, settings.Threshold);
                bitmap.UnlockBits(data);

                if (cropAnalyzer.BorderIsDirty && !analyzer.BorderIsDirty && analyzer.BucketRatio > 0.9)
                {
                    var dimension = (int)((bitmap.Width + bitmap.Height) * 0.25f);
                    var paddingX = GetPadding(settings.PadX, dimension);
                    var paddingY = GetPadding(settings.PadY, dimension);

                    var boxPadding = new BoxPadding(paddingX, paddingY, paddingX, paddingY);
                    var result = new AutoPadState(boxPadding, analyzer.BackgroundColor);

                    state.Data[DataKey] = result;
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected override RequestedAction PreRenderImage(ImageState state)
        {
            if (state == null || !state.Data.ContainsKey(DataKey))
                return RequestedAction.None;

            try
            {
                var data = (AutoPadState)state.Data[DataKey];
                var padding = data.Padding;
                var bitmap = state.sourceBitmap;

                var paddingX = (int)padding.Left;
                var paddingY = (int)padding.Top;

                var width = bitmap.Width + paddingX * 2;
                var height = bitmap.Height + paddingY * 2;

                if (state.preRenderBitmap == null)
                    state.preRenderBitmap = new Bitmap(width, height);

                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    using (var brush = new SolidBrush(data.BackgroundColor))
                    {
                        graphics.FillRectangle(brush, new Rectangle(0, 0, width, height));
                    }

                    var from = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                    var to = new Rectangle(paddingX, paddingY, bitmap.Width - paddingX * 2, bitmap.Height - paddingY * 2);

                    graphics.DrawImage(bitmap, to, from, GraphicsUnit.Pixel);
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected int GetPadding(int percentage, int dimension)
        {
            return (int)Math.Ceiling(dimension * percentage * 0.01f);
        }

        protected AutoPadSettings ParseSettings(string settingsValue)
        {
            var result = new AutoPadSettings();
            if (settingsValue == null) return result;

            var data = settingsValue.Split(',', ';', '|');

            var parsed = int.TryParse(data[0], out var padX);
            if (!parsed) return result;

            result.Parsed = true;
            result.PadX = Math.Max(padX, 0);

            if (data.Length > 2 && int.TryParse(data[2], out var threshold))
            {
                result.Threshold = Math.Max(threshold, 0);
            }

            if (data.Length > 1 && int.TryParse(data[1], out var padY))
            {
                result.PadY = Math.Max(padY, 0);
            }
            else
            {
                result.PadY = Math.Max(padX, 0);
            }

            return result;
        }

        protected bool IsRequiredSize(Bitmap bitmap)
        {
            if (bitmap == null) return false;
            return IsRequiredSize(bitmap.Width, bitmap.Height);
        }

        protected bool IsRequiredSize(int width, int height)
        {
            if (width < 4 && height <= 4) return false;
            if (height < 4 && width <= 4) return false;

            if (width < 3) return false;
            if (height < 3) return false;

            return true;
        }

        protected bool IsCorrectFormat(PixelFormat format)
        {
            var bitsPerPixel = Image.GetPixelFormatSize(format) / 8;
            if (bitsPerPixel < 3) return false;
            if (bitsPerPixel > 4) return false;

            return true;
        }
    }
}
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using ImageResizer.Configuration;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Resizing;
using AutoCrop.Core.Analyzers;
using AutoCrop.Core.Extensions;
using AutoCrop.Core.Models;

namespace ImageResizer.Plugins.AutoCrop
{
    public class AutoCropPlugin : BuilderExtension, IPlugin, IQuerystringPlugin
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
                "autoCrop",
                "autoCropMode",
                "autoCropDebug"
            };
        }

        public readonly string DataKey = "autocrop";
        public readonly string SettingsKey = "autocropsettings";
        public readonly string DebugKey = "autocropdebug";

        protected override RequestedAction PostPrepareSourceBitmap(ImageState state)
        {
            if (state == null) return RequestedAction.None;
            if (state.settings == null) return RequestedAction.None;
            if (state.sourceBitmap == null) return RequestedAction.None;

            var bitmap = state.sourceBitmap;
            if (!IsRequiredSize(bitmap)) return RequestedAction.None;

            var pixelFormat = bitmap.PixelFormat;
            if (!IsCorrectFormat(pixelFormat)) return RequestedAction.None;

            var setting = state.settings["autoCrop"];
            if (setting == null) return RequestedAction.None;

            var settings = ParseSettings(setting, state.settings["autoCropMode"], state.settings["autoCropDebug"]);

            state.Data[SettingsKey] = settings;

            try
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var analyzer = new BoundsAnalyzer(data, settings.Threshold);
                bitmap.UnlockBits(data);

                if (analyzer.FoundBoundingBox)
                {
                    state.Data[DataKey] = new BoundsAnalysisResult(analyzer);
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected override RequestedAction LayoutImage(ImageState state)
        {
            if (state == null || !state.Data.ContainsKey(DataKey) || !state.Data.ContainsKey(SettingsKey))
                return RequestedAction.None;

            try
            {
                var bitmap = state.sourceBitmap;
                var settings = (AutoCropSettings)state.Data[SettingsKey];
                var data = (BoundsAnalysisResult)state.Data[DataKey];
                
                var bounds = data.Bounds;
                var targetMode = settings.SetMode ? settings.Mode : state.settings.Mode;

                var dimension = (int)((bounds.Width + bounds.Height) * 0.25f);
                var paddingX = GetPadding(settings.PadX, dimension);
                var paddingY = GetPadding(settings.PadY, dimension);
                var paddedBox = bounds.Expand(paddingX, paddingY, bitmap.Width, bitmap.Height);

                var destinationSize = GetDestinationSize(state, bitmap);
                var destinationAspect = destinationSize.Width / (float)destinationSize.Height;

                var targetBox = paddedBox.ConstrainAspect(destinationAspect, bitmap.Width, bitmap.Height);

                if (settings.Debug)
                {
                    state.Data[DebugKey] = bounds;
                }
                else
                {
                    state.originalSize = targetBox.Size;

                    if (settings.SetMode)
                    {
                        state.settings.Mode = settings.Mode;
                    }

                    if (state.settings.BackgroundColor.Equals(Color.Transparent))
                    {
                        state.settings.BackgroundColor = data.BorderColor;
                    }
                }

                state.Data[DataKey] = targetBox;
            }
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected override RequestedAction PostLayoutImage(ImageState state)
        {
            if (state == null || !state.Data.ContainsKey(DataKey) || state.Data.ContainsKey(DebugKey)) 
                return RequestedAction.None;

            var box = (Rectangle)state.Data[DataKey];
            var copyRect = new RectangleF(state.copyRect.X + box.X, state.copyRect.Y + box.Y, state.copyRect.Width, state.copyRect.Height);

            state.copyRect = copyRect;

            return RequestedAction.None;
        }

        protected override RequestedAction PreRenderImage(ImageState state)
        {
            if (state == null || !state.Data.ContainsKey(DataKey) || !state.Data.ContainsKey(DebugKey))
                return RequestedAction.None;
            
            try
            {
                if (state.preRenderBitmap == null)
                    state.preRenderBitmap = new Bitmap(state.sourceBitmap);
                
                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var pen = new Pen(Color.CornflowerBlue, 2))
                    {
                        graphics.DrawRectangle(pen, (Rectangle)state.Data[DataKey]);
                    }

                    using (var pen = new Pen(Color.Red, 2))
                    {
                        graphics.DrawRectangle(pen, (Rectangle)state.Data[DebugKey]);
                    }
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
            return (int) Math.Ceiling(dimension * percentage * 0.01f);
        }

        protected Size GetDestinationSize(ImageState state, Bitmap bitmap)
        {
            var originalSize = new Size(bitmap.Width, bitmap.Height);

            var w = state.settings["width"];
            var h = state.settings["height"];

            if (w == null || !int.TryParse(w, out var width))
                return originalSize;

            if (h == null || !int.TryParse(h, out var height))
                return originalSize;

            if (!IsRequiredSize(width, height))
                return originalSize;

            return new Size(width, height);
        }

        protected AutoCropSettings ParseSettings(string settingsValue, string modeValue = null, string debugValue = null)
        {
            var result = new AutoCropSettings();
            if (settingsValue == null) return result;

            result.Debug = debugValue != null;
            
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

            if (Enum.TryParse(modeValue, true, out FitMode fitMode))
            {
                result.SetMode = true;
                result.Mode = fitMode;
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
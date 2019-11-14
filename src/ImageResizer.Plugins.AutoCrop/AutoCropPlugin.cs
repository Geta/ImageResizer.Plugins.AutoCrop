using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using ImageResizer.Configuration;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Resizing;
using ImageResizer.Plugins.AutoCrop.Analyzers;
using ImageResizer.Plugins.AutoCrop.Extensions;

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
                var analyzer = new BoundsAnalyzer(data, settings.Threshold, 0.90f);

                bitmap.UnlockBits(data);

                if (analyzer.FoundBoundingBox)
                {
                    state.Data[DataKey] = new AutoCropState(analyzer, bitmap);
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
                var data = (AutoCropState)state.Data[DataKey];
                
                var bounds = data.Bounds;
                var targetMode = settings.SetMode ? settings.Mode : state.settings.Mode;

                var dimension = (int)((bounds.Width + bounds.Height) * 0.25f);
                var paddingX = GetPadding(settings.PadX, dimension);
                var paddingY = GetPadding(settings.PadY, dimension);
                var paddedBox = bounds.Expand(paddingX, paddingY);

                data.Padding = new Size(paddingX, paddingY);

                var destinationSize = GetDestinationSize(state, bitmap);
                var destinationAspect = destinationSize.Width / (float)destinationSize.Height;

                var targetBox = data.TargetDimensions = paddedBox.Aspect(destinationAspect);

                data.Scale = destinationSize.Width / (float)targetBox.Width;

                if (settings.Debug)
                {
                    state.Data[DebugKey] = bounds;
                }
                else
                {
                    if (data.OriginalDimensions.Contains(targetBox))
                    {
                        state.originalSize = targetBox.Size;

                        if (settings.SetMode)
                        {
                            state.settings.Mode = settings.Mode;
                        }
                    }
                    else
                    {
                        state.settings.Mode = FitMode.Crop;
                    }
                    
                    if (state.settings.BackgroundColor.Equals(Color.Transparent))
                    {
                        state.settings.BackgroundColor = data.BorderColor;
                    }
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
            
            var data = (AutoCropState)state.Data[DataKey];
            var originalDimensions = data.OriginalDimensions;
            var targetDimensions = data.TargetDimensions;

            var source = data.Bounds;
            var padding = data.Padding;
            var scale = data.Scale;

            if (originalDimensions.Contains(targetDimensions))
            {
                var copyRect = new RectangleF(state.copyRect.X + targetDimensions.X, state.copyRect.Y + targetDimensions.Y, state.copyRect.Width, state.copyRect.Height);
                state.copyRect = copyRect;
            }
            else
            {
                var w = (int)Math.Ceiling(targetDimensions.Width * scale);
                var h = (int)Math.Ceiling(targetDimensions.Height * scale);

                if (state.preRenderBitmap == null)
                    state.preRenderBitmap = new Bitmap(w, h);

                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.HighQuality;

                    using (var brush = new SolidBrush(state.settings.BackgroundColor))
                    {
                        graphics.FillRectangle(brush, new Rectangle(0, 0, w, h));
                    }

                    var x = (int)Math.Ceiling((targetDimensions.Width - source.Width) * 0.5f * scale);
                    var y = (int)Math.Ceiling((targetDimensions.Height - source.Height) * 0.5f * scale);

                    var dw = (int)Math.Ceiling(source.Width * scale);
                    var dh = (int)Math.Ceiling(source.Height * scale);

                    var destination = new Rectangle(x, y, dw, dh);

                    graphics.DrawImage(state.sourceBitmap, destination, source, GraphicsUnit.Pixel);
                    source = destination;
                }

                state.originalSize = new Size(w, h);
                state.copyRect = new RectangleF(0, 0, w, h);
            }

            if (state == null || !state.Data.ContainsKey(DebugKey))
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
                        graphics.DrawRectangle(pen, source);
                    }

                    using (var pen = new Pen(Color.Red, 2))
                    {
                        graphics.DrawRectangle(pen, source.Expand(padding.Width, padding.Height));
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
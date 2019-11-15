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
                var analyzer = new BoundsAnalyzer(data, settings.Threshold, 0.945f);

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
                
                data.Padding = new Size(paddingX, paddingY);

                var paddedBox = bounds.Expand(paddingX, paddingY);

                var destinationSize = GetDestinationSize(state, bitmap);
                var destinationAspect = destinationSize.Width / (float)destinationSize.Height;

                var targetBox = data.TargetDimensions = paddedBox.Aspect(destinationAspect);
                var scale = destinationSize.Width / (double)targetBox.Width;

                if (settings.Debug)
                {
                    state.Data[DebugKey] = bounds;
                }
                else
                {
                    if (data.OriginalDimensions.Contains(targetBox))
                    {
                        state.originalSize = targetBox.Size;
                    }
                    else
                    {
                        var w = (int)Math.Min(Math.Ceiling(targetBox.Width * scale), destinationSize.Width);
                        var h = (int)Math.Min(Math.Ceiling(targetBox.Height * scale), destinationSize.Height);

                        var x = (int)Math.Ceiling((w - bounds.Width * scale) * 0.5f);
                        var y = (int)Math.Ceiling((h - bounds.Height * scale) * 0.5f);

                        var dw = (int)Math.Ceiling(bounds.Width * scale);
                        var dh = (int)Math.Ceiling(bounds.Height * scale);

                        var destination = new Rectangle(x, y, dw, dh);
                        var size = new Size(w, h);

                        state.preRenderBitmap = new Bitmap(w, h, bitmap.PixelFormat);
                        state.originalSize = size;
                        state.copyRect = new Rectangle(0, 0, w, h);

                        data.ShouldPreRender = true;
                        data.PreRenderInstructions = new RenderInstructions
                        {
                            Size = size,
                            Source = bounds,
                            Destination = destination
                        };
                    }

                    if (settings.SetMode)
                    {
                        state.settings.Mode = settings.Mode;
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
            var bounds = data.Bounds;
           
            if (data.ShouldPreRender) 
            {
                var instructions = data.PreRenderInstructions;
                var size = instructions.Size;
                var source = instructions.Source;
                var destination = instructions.Destination;

                if (state.preRenderBitmap == null || state.preRenderBitmap.Size != size)
                    state.preRenderBitmap = new Bitmap(size.Width, size.Height, state.sourceBitmap.PixelFormat);

                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.CompositingMode = CompositingMode.SourceOver;

                    using (var brush = new SolidBrush(data.BorderColor))
                    {
                        graphics.FillRectangle(brush, new Rectangle(new Point(0, 0), size));
                    }

                    graphics.DrawImage(state.sourceBitmap, destination, source, GraphicsUnit.Pixel);
                    bounds = destination;
                }
            }
            else
            {
                var targetBox = data.TargetDimensions;
                state.copyRect = new RectangleF(state.copyRect.X + targetBox.X, state.copyRect.Y + targetBox.Y, state.copyRect.Width, state.copyRect.Height);
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

                    var padding = data.Padding;

                    using (var pen = new Pen(Color.CornflowerBlue, 2))
                    {
                        graphics.DrawRectangle(pen, bounds);
                    }

                    using (var pen = new Pen(Color.Red, 2))
                    {
                        graphics.DrawRectangle(pen, bounds.Expand(padding.Width, padding.Height));
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
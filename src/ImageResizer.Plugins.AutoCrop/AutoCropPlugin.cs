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
using ImageResizer.Plugins.AutoCrop.Actions;

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

        protected override RequestedAction PostPrepareSourceBitmap(ImageState state)
        {
            if (state == null) return RequestedAction.None;
            if (state.settings == null) return RequestedAction.None;
            if (state.sourceBitmap == null) return RequestedAction.None;

            var bitmap = state.sourceBitmap;
            if (!IsRequiredSize(bitmap)) return RequestedAction.None;

            var colorFormat = bitmap.GetColorFormat();
            if (!IsCorrectColorFormat(colorFormat)) return RequestedAction.None;

            var pixelFormat = bitmap.PixelFormat;
            if (!IsCorrectPixelFormat(pixelFormat)) return RequestedAction.None;

            var setting = state.settings["autoCrop"];
            if (setting == null) return RequestedAction.None;

            var settings = ParseSettings(setting, state.settings["autoCropMode"], state.settings["autoCropDebug"]);

            state.Data[SettingsKey] = settings;

            try
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var analyzer = GetAnalyzer(data, settings);
                var analysis = analyzer.GetAnalysis();

                bitmap.UnlockBits(data);

                if (analysis.Success)
                {
                    state.Data[DataKey] = new AutoCropState(analysis, bitmap);
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

                if (data.OriginalDimensions.Contains(targetBox))
                {
                    state.originalSize = targetBox.Size;

                    data.ShouldPreRender = false;
                    data.Instructions = new RenderInstructions
                    {
                        Translate = new Point(0, 0),
                        Source = bounds,
                        Target = paddedBox,
                        Scale = scale,
                        Size = new Size(bitmap.Width, bitmap.Height)
                    };
                }
                else
                {
                    var w = paddedBox.Width;
                    var h = paddedBox.Height;

                    var size = new Size(w, h);
                    var targetW = w;
                    var targetH = h;

                    var offsetX = paddedBox.X;
                    var offsetY = paddedBox.Y;
                        
                    var translateX = 0;
                    var translateY = 0;
                        
                    if (offsetX < 0)
                    {
                        offsetX = 0;
                        translateX -= paddedBox.X;
                        targetW = w + paddedBox.X;
                    }

                    if (offsetY < 0)
                    {
                        offsetY = 0;
                        translateY -= paddedBox.Y;
                        targetH = h + paddedBox.Y;
                    }

                    var overlapX = paddedBox.X + w - bitmap.Width;
                    var overlapY = paddedBox.Y + h - bitmap.Height;

                    if (overlapX > 0)
                    {
                        targetW -= overlapX;
                    }

                    if (overlapY > 0)
                    {
                        targetH -= overlapY;
                    }

                    state.preRenderBitmap = new Bitmap(w, h, bitmap.PixelFormat);
                    state.originalSize = size;

                    data.ShouldPreRender = true;
                    data.Instructions = new RenderInstructions
                    {
                        Size = size,
                        Source = bounds,
                        Scale = scale,
                        Translate = new Point(translateX, translateY),
                        Target = new Rectangle(new Point(offsetX, offsetY), new Size(targetW, targetH))
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
            var settings = (AutoCropSettings)state.Data[SettingsKey];
            var targetBox = data.TargetDimensions;
            var instructions = data.Instructions;
            var size = instructions.Size;

            if (data.ShouldPreRender) 
            {                
                if (state.preRenderBitmap == null || state.preRenderBitmap.Size != size)
                    state.preRenderBitmap = new Bitmap(size.Width, size.Height, state.sourceBitmap.PixelFormat);

                if (data.BitsPerPixel == 3)
                {
                    Raw.FillRgb(state.preRenderBitmap, data.BorderColor);
                }
                else if (data.BitsPerPixel == 4)
                {
                    Raw.FillRgba(state.preRenderBitmap, data.BorderColor);
                }

                Raw.Copy(state.sourceBitmap, instructions.Target, state.preRenderBitmap, instructions.Translate);

                state.copyRect = new RectangleF(0, 0, size.Width, size.Height);
            }
            else
            {
                state.copyRect = new RectangleF(state.copyRect.X + targetBox.X, state.copyRect.Y + targetBox.Y, state.copyRect.Width, state.copyRect.Height);
            }

            if (state == null || !settings.Debug)
                return RequestedAction.None;

            try
            {
                if (state.preRenderBitmap == null)
                    state.preRenderBitmap = new Bitmap(state.sourceBitmap);
                
                state.preRenderBitmap = Filter.Sobel(state.preRenderBitmap);

                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var pen = new Pen(Color.Red, 4))
                    {
                        var rectangle = instructions.Source;
                        
                        if (data.ShouldPreRender)
                        {
                            var padding = data.Padding;
                            var width = size.Width - padding.Width * 2;
                            var height = size.Height - padding.Height * 2;
                            var offsetX = padding.Width;
                            var offsetY = padding.Height;

                            rectangle = new Rectangle(offsetX, offsetY, width, height);
                        }                           

                        graphics.DrawRectangle(pen, rectangle);
                    }
                }
            }
            catch (Exception ex)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected virtual IAnalyzer GetAnalyzer(BitmapData data, AutoCropSettings settings)
        {
            return new BoundsAnalyzer(data, settings.Threshold, 0.945f);
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

        protected static bool IsRequiredSize(int width, int height)
        {
            if (width < 4 && height <= 4) return false;
            if (height < 4 && width <= 4) return false;

            if (width < 3) return false;
            if (height < 3) return false;

            return true;
        }

        protected static bool IsCorrectColorFormat(ImageColorFormat format)
        {
            if (format == ImageColorFormat.Cmyk) return false;
            if (format == ImageColorFormat.Grayscale) return false;
            if (format == ImageColorFormat.Indexed) return false;

            return true;
        }

        protected static bool IsCorrectPixelFormat(PixelFormat format)
        {
            var bitsPerPixel = Image.GetPixelFormatSize(format) / 8;
            if (bitsPerPixel < 3) return false;
            if (bitsPerPixel > 4) return false;

            return true;
        }
    }
}
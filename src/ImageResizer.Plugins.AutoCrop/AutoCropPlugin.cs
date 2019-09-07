using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Collections.Generic;
using ImageResizer.Configuration;
using ImageResizer.Plugins.AutoCrop.Analyzers;
using ImageResizer.Plugins.AutoCrop.Models;
using ImageResizer.Resizing;

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
                "autoCropDebug"
            };
        }

        public readonly string DataKey = "autocrop";
        public readonly string SettingsKey = "autocropsettings";
        public readonly string DebugKey = "autocropdebug";
        
        protected override RequestedAction LayoutImage(ImageState state)
        {
            var action = base.PostLayoutImage(state);
            if (action == RequestedAction.Cancel) return RequestedAction.Cancel;

            var enabled = DetermineEnabled(state);
            if (!enabled) return RequestedAction.None;

            var settings = GetSettings(state);
            var bitmap = state.sourceBitmap;

            state.Data[SettingsKey] = settings;

            try
            {
                var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
                var analyzer = new BoundsAnalyzer(data, settings.Threshold);
                
                bitmap.UnlockBits(data);

                if (analyzer.FoundBoundingBox)
                {
                    var aspectCorrectedBox = GetConstrainedAspect(analyzer.BoundingBox, bitmap.Width, bitmap.Height);

                    var dimension = (int)((aspectCorrectedBox.Width + aspectCorrectedBox.Height) * 0.25f); 
                    var paddingX = GetPadding(settings.PadX, dimension);
                    var paddingY = GetPadding(settings.PadY, dimension);
                    
                    var paddedBox = ExpandRectangle(analyzer.BoundingBox, paddingX, paddingY, bitmap.Width, bitmap.Height);
                    var constrainedBox = GetConstrainedAspect(paddedBox, bitmap.Width, bitmap.Height);

                    if (settings.Debug)
                    {
                        state.Data[DebugKey] = analyzer.BoundingBox;                        
                    }
                    else
                    {
                        state.originalSize = constrainedBox.Size;
                    }

                    state.Data[DataKey] = constrainedBox;
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected override RequestedAction PostLayoutImage(ImageState state)
        {
            if (!state.Data.ContainsKey(DataKey) || state.Data.ContainsKey(DebugKey)) 
                return RequestedAction.None;

            var box = (Rectangle)state.Data[DataKey];
            var copyRect = new RectangleF(state.copyRect.X + box.X, state.copyRect.Y + box.Y, state.copyRect.Width, state.copyRect.Height);

            state.copyRect = copyRect;

            return RequestedAction.None;
        }

        protected override RequestedAction PreRenderImage(ImageState state)
        {
            if (!state.Data.ContainsKey(DataKey) || !state.Data.ContainsKey(DebugKey))
                return RequestedAction.None;
            
            try
            {
                if (state.preRenderBitmap == null)
                    state.preRenderBitmap = new Bitmap(state.sourceBitmap);
                
                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var pen = new Pen(Color.Red, 2))
                    {
                        graphics.DrawRectangle(pen, (Rectangle)state.Data[DebugKey]);
                    }

                    using (var pen = new Pen(Color.CornflowerBlue, 2))
                    {
                        graphics.DrawRectangle(pen, (Rectangle)state.Data[DataKey]);
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

        protected Rectangle GetConstrainedAspect(Rectangle rectangle, int width, int height)
        {
            var oa = width / (float)height;
            var ta = rectangle.Width / (float)rectangle.Height;

            if (Math.Abs(oa - ta) < 0.01f)
                return rectangle;

            if (oa > ta)
            {
                var iw = rectangle.Height * oa;
                var p = (int) Math.Ceiling((iw - rectangle.Width) * 0.5f);
                return ExpandRectangle(rectangle, p, 0, width, height);
            }
            else
            {
                var ih = rectangle.Width / oa;
                var p = (int) Math.Ceiling((ih - rectangle.Height) * 0.5f);
                return ExpandRectangle(rectangle, 0, p, width, height);
            }
        }

        protected Rectangle ExpandRectangle(Rectangle rectangle, int paddingX, int paddingY, int width, int height)
        {
            if (paddingX == 0 && paddingY == 0) return rectangle;

            var xn = Math.Max(0, rectangle.X - paddingX);
            var xm = Math.Min(width, rectangle.Right + paddingX);

            var yn = Math.Max(0, rectangle.Y - paddingY);
            var ym = Math.Min(height, rectangle.Bottom + paddingY);

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }

        protected bool DetermineEnabled(ImageState state)
        {
            if (state.settings == null) return false;

            var pixelFormat = state.sourceBitmap.PixelFormat;

            if (!IsCorrectFormat(pixelFormat)) return false;

            var setting = state.settings["autoCrop"];
            if (setting == null) return false;

            return true;
        }

        protected AutoCropSettings GetSettings(ImageState state)
        {
            var result = new AutoCropSettings();
            var raw = state.settings["autoCrop"];
            if (raw == null) return result;

            result.Debug = state.settings["autoCropDebug"] != null;
            
            var data = raw.Split(',', ';', '|');

            var parsed = int.TryParse(data[0], out var padX);
            if (!parsed) return result;

            result.Parsed = true;
            result.PadX = padX;

            if (data.Length > 2 && int.TryParse(data[2], out var threshold))
            {
                result.Threshold = threshold;
            }

            if (data.Length > 1 && int.TryParse(data[1], out var padY))
            {
                result.PadY = padY;
            }
            else
            {
                result.PadY = padX;
            }
            
            return result;
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
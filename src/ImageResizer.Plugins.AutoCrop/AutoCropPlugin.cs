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
                "autoCropMethod",
                "autoCropDebug"
            };
        }

        public readonly string DataKey = "autocrop";
        public readonly string SettingsKey = "autocropsettings";

        protected override RequestedAction PostPrepareSourceBitmap(ImageState state)
        {
            if (!QualifyState(state))
                return RequestedAction.None;

            // Parse plugin settings
            var settings = ParseSettings(state.settings);
            if (settings == null)
                return RequestedAction.None;

            var bitmap = state.sourceBitmap;
            var data = (BitmapData)null;

            state.Data[SettingsKey] = settings;

            try
            {
                // Lock memory range for bitmap for read purposes
                data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

                // Get analyzer depending on settings
                var analyzer = GetAnalyzer(data, settings);

                // The analyzer determines if the image is croppable...
                // ...also which area of the image to scan
                var analysis = analyzer.GetAnalysis();

                if (analysis.Success)
                {
                    // Add an AutoCropState for later use
                    state.Data[DataKey] = new AutoCropState(analysis, bitmap);
                }
            }
            catch (Exception)
            {
                // Felt cute, might implement logging later
                // Without the DataKey, later processing steps will not occur
            }
            finally
            {
                // Unlock memory access
                if (data != null)
                    bitmap.UnlockBits(data);
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

                // Average dimension of half the image width and height
                var dimension = (int)((bounds.Width + bounds.Height) * 0.25f);

                // Calculate the padding in pixels based on dimension
                // (because padding is set in percentage)
                var paddingX = GetPadding(settings.PadX, dimension);
                var paddingY = GetPadding(settings.PadY, dimension);
                
                data.Padding = new Size(paddingX, paddingY);

                // Pad the analyzed crop bounds
                var paddedBox = bounds.Expand(paddingX, paddingY);

                // Returns target size if both widht/height resize paramters are set
                // Returns source size if only one (e.g. width)
                var destinationSize = GetDestinationSize(state, bitmap);

                // Calculate targeted aspect ratio
                var destinationAspect = destinationSize.Width / (float)destinationSize.Height;
                
                // Expand the padded box with the calculated final aspect ratio
                var targetBox = data.TargetDimensions = paddedBox.ForceAspect(destinationAspect);
                var scale = destinationSize.Width / (double)targetBox.Width;
                var originalSize = new Size(bitmap.Width, bitmap.Height);

                if (data.OriginalDimensions.Contains(targetBox))
                {
                    // Modify original size in ImageResizer state
                    // so that the layout process thinks the image is the target size
                    state.originalSize = targetBox.Size;

                    // In this case the image does not need to be re-rendered
                    // It's sufficient to change the image state to get the desired crop
                    data.ShouldPreRender = false;

                    // Calculate cropping instructions
                    data.Instructions = GetContainedInstructions(bounds, paddedBox, originalSize, scale);
                }
                else
                {
                    var size = new Size(paddedBox.Width, paddedBox.Height);

                    // Same trick as previous scenario but using the padded box instead
                    state.originalSize = size;

                    // Create a preRender bitmap beforehand this way there won't be exceptions later
                    state.preRenderBitmap = new Bitmap(size.Width, size.Height, bitmap.PixelFormat);

                    // In this case the padded box has expanded outside the image area
                    // This image needs to be re-rendered with the appropriate whitespace
                    data.ShouldPreRender = true;

                    // Calculate cropping instructions
                    data.Instructions = GetUncontainedInstructions(bounds, paddedBox, originalSize, scale);
                }

                // If a special FitMode has been instructed in the settings now is the time to set it
                SetMode(state, data, settings);

                // If no background color has been set we set the background to that of the analysis
                // so that if the image is padded via instruction, the added background color will fit
                // If debug mode is set, special backgrounds are assigned
                SetBackground(state, data, settings);
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
                // In this case another image should be rendered
                // This can be done by using the preRenderBitmap in the image state
                // If there isn't one already, or if it is of differing size, override it
                if (state.preRenderBitmap == null || state.preRenderBitmap.Size != size)
                    state.preRenderBitmap = new Bitmap(size.Width, size.Height, state.sourceBitmap.PixelFormat);

                // Custom fill routine
                if (data.BytesPerPixel == 3)
                {
                    Raw.FillRgb(state.preRenderBitmap, data.BorderColor);
                }
                else if (data.BytesPerPixel == 4)
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

                if (settings.Method == AutoCropMethod.Edge)
                {
                    // Display graphics as analyzer sees it.
                    state.preRenderBitmap = Filter.Sobel(state.preRenderBitmap);   
                }
                else
                {
                    state.preRenderBitmap = Filter.Buckets(state.preRenderBitmap);
                }                

                // Establish a drawing canvas
                using (var graphics = Graphics.FromImage(state.preRenderBitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    // Render the detected bounding box in red
                    var brushSize = 2 * (1 / instructions.Scale);

                    using (var pen = new Pen(Color.Red, (int)brushSize))
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
            catch (Exception)
            {
                // ignore
            }

            return RequestedAction.None;
        }

        protected virtual RenderInstructions GetContainedInstructions(Rectangle bounds, Rectangle target, Size originalSize, double scale)
        {
            return new RenderInstructions
            {
                Translate = new Point(0, 0),
                Source = bounds,
                Target = target,
                Scale = scale,
                Size = originalSize
            };
        }

        protected virtual RenderInstructions GetUncontainedInstructions(Rectangle bounds, Rectangle target, Size originalSize, double scale)
        {
            var w = target.Width;
            var h = target.Height;

            var size = new Size(w, h);
            var targetW = w;
            var targetH = h;

            var offsetX = target.X;
            var offsetY = target.Y;
                        
            var translateX = 0;
            var translateY = 0;
                        
            if (offsetX < 0)
            {
                offsetX = 0;
                translateX -= target.X;
                targetW = w + target.X;
            }

            if (offsetY < 0)
            {
                offsetY = 0;
                translateY -= target.Y;
                targetH = h + target.Y;
            }

            var overlapX = target.X + w - originalSize.Width;
            var overlapY = target.Y + h - originalSize.Height;

            if (overlapX > 0)
            {
                targetW -= overlapX;
            }

            if (overlapY > 0)
            {
                targetH -= overlapY;
            }

            return new RenderInstructions
            {
                Translate = new Point(translateX, translateY),
                Source = bounds,
                Target = new Rectangle(new Point(offsetX, offsetY), new Size(targetW, targetH)),
                Scale = scale,
                Size = size
            };
        }

        protected virtual void SetMode(ImageState state, AutoCropState data, AutoCropSettings settings)
        {
            if (settings.SetMode)
            {
                state.settings.Mode = settings.Mode;
            }
        }

        protected virtual void SetBackground(ImageState state, AutoCropState data, AutoCropSettings settings)
        {
            if (settings.Debug)
            {
                if (settings.Method == AutoCropMethod.Edge)
                {
                    state.settings.BackgroundColor = Color.FromArgb(255, 0, 0, 0);
                }
                else
                {
                    var backgroundColor = state.settings.BackgroundColor;
                    state.settings.BackgroundColor = backgroundColor.ToColorBucket().ToColor();
                }
            }

            if (state.settings.BackgroundColor.Equals(Color.Transparent))
            {
                state.settings.BackgroundColor = data.BorderColor;
            }
        }

        protected virtual IAnalyzer GetAnalyzer(BitmapData data, AutoCropSettings settings)
        {
            if (settings.Method == AutoCropMethod.Edge)
                return new SobelAnalyzer(data, settings.Threshold, 0.945f);
            
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

        protected AutoCropSettings ParseSettings(ResizeSettings settings)
        {
            var settingsValue = settings["autoCrop"];
            if (settingsValue == null) 
                return null;

            var modeValue = settings["autoCropMode"];
            var methodValue = settings["autoCropMethod"];
            var debugValue = settings["autoCropDebug"];

            var result = new AutoCropSettings
            {
                Debug = debugValue != null
            };

            var data = settingsValue.Split(',', ';', '|');
            var parsed = int.TryParse(data[0], out var padX);
            if (!parsed) 
                return result;

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

            if (Enum.TryParse(methodValue, true, out AutoCropMethod autoCropMethod))
            {
                result.Method = autoCropMethod;
            }

            if (Enum.TryParse(modeValue, true, out FitMode fitMode))
            {
                result.SetMode = true;
                result.Mode = fitMode;
            }
            
            return result;
        }

        protected virtual bool QualifyState(ImageState state)
        {
            if (state?.settings == null) 
                return false;

            if (state.sourceBitmap == null) 
                return false;

            var bitmap = state.sourceBitmap;
            if (!IsRequiredSize(bitmap)) 
                return false;

            var colorFormat = bitmap.GetColorFormat();
            if (!IsCorrectColorFormat(colorFormat)) 
                return false;

            var pixelFormat = bitmap.PixelFormat;
            if (!IsCorrectPixelFormat(pixelFormat)) 
                return false;

            return true;
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
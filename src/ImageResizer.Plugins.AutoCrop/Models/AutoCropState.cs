using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class AutoCropState
    {
        public readonly Rectangle Bounds;
        public readonly Color BorderColor;
        public readonly Rectangle OriginalDimensions;
        public Rectangle TargetDimensions;
        public Size Padding;
        public int BytesPerPixel;

        public bool ShouldPreRender;
        public RenderInstructions Instructions;        

        public AutoCropState(IAnalysis analysis, Bitmap bitmap)
        {
            Bounds = analysis.BoundingBox;
            BytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            BorderColor = analysis.Background;
            OriginalDimensions = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        }
    }
}

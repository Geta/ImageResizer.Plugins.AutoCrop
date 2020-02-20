using ImageResizer.Plugins.AutoCrop.Analyzers;
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
        public int BitsPerPixel;

        public bool ShouldPreRender;
        public RenderInstructions Instructions;        

        public AutoCropState(BoundsAnalyzer analyzer, Bitmap bitmap)
        {
            Bounds = analyzer.BoundingBox;
            BitsPerPixel = analyzer.BorderAnalysis.BitsPerPixel;
            BorderColor = analyzer.BorderAnalysis.BackgroundColor;
            OriginalDimensions = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        }
    }
}

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

        public bool ShouldPreRender;
        public RenderInstructions PreRenderInstructions;        

        public AutoCropState(BoundsAnalyzer analyzer, Bitmap bitmap)
        {
            Bounds = analyzer.BoundingBox;
            BorderColor = analyzer.BorderAnalysis.BackgroundColor;
            OriginalDimensions = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        }
    }
}

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
        public PointF Weight;
        public int BytesPerPixel;

        public bool ShouldPreRender;
        public RenderInstructions Instructions;        

        public AutoCropState(Bitmap bitmap, ICropAnalysis cropAnalysis = null, IWeightAnalysis weightAnalysis = null)
        {
            var originalDimensions = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            BytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            BorderColor = cropAnalysis?.Background ?? Color.White;
            Weight = weightAnalysis?.Weight ?? new PointF(0, 0);
            Bounds = cropAnalysis?.BoundingBox ?? originalDimensions;
            OriginalDimensions = originalDimensions;
        }
    }
}

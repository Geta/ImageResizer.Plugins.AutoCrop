using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public interface ICropAnalysis
    {
        Rectangle BoundingBox { get; }
        Color Background { get; }
        bool Success { get; }
    }
}

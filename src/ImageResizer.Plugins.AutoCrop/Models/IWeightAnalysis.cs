using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public interface IWeightAnalysis
    {
        PointF Weight { get; set; }
    }
}

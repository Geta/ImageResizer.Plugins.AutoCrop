using ImageResizer.Plugins.AutoCrop.Models;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public interface IWeightAnalyzer
    {
        IWeightAnalysis GetAnalysis();
    }
}
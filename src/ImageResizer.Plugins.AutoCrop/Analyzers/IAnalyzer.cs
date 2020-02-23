using ImageResizer.Plugins.AutoCrop.Models;

namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public interface IAnalyzer
    {
        IAnalysis GetAnalysis();
    }
}

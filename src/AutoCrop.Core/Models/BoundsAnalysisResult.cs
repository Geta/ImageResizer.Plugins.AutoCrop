using AutoCrop.Core.Analyzers;
using System.Drawing;

namespace AutoCrop.Core.Models
{
    public class BoundsAnalysisResult
    {
        public readonly Rectangle Bounds;
        public readonly Color BorderColor;

        public BoundsAnalysisResult(BoundsAnalyzer analyzer)
        {
            Bounds = analyzer.BoundingBox;
            BorderColor = analyzer.BorderAnalysis.BackgroundColor;
        }
    }
}

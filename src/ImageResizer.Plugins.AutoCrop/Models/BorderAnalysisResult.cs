using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class BorderAnalysisResult
    {
        public BorderAnalysisResult()
        {
        }

        public BorderAnalysisResult(IDictionary<Color, int> colors, int threshold)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));

            Colors = colors.Count;
            Success = colors.Count > 0 && colors.Count < threshold;

            if (!Success)
            {
                Background = Color.Transparent;
            }
            else
            {
                var mostPresentColor = colors.OrderByDescending(x => x.Value)
                                             .First();

                Background = mostPresentColor.Key;
            }
        }

        public readonly int Colors;
        public readonly Color Background;
        public readonly bool Success;
    }
}

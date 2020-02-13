using ImageResizer.Plugins.AutoCrop.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class SnakeAnalysisResult
    {

        public SnakeAnalysisResult()
        {
            Colors = 0;
            Background = Color.Transparent;
            BackgrounRatio = 0;
            Difference = 0;
        }

        public SnakeAnalysisResult(IDictionary<Color, int> colors, IDictionary<int, int> buckets, byte maxDifference)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));

            Colors = colors.Count;
            Difference = maxDifference;

            var mostPresentColor = colors.OrderByDescending(x => x.Value)
                                         .First();

            var mostPresentBucket = mostPresentColor.Key.ToColorBucket();

            Background = mostPresentColor.Key;
            BackgrounRatio = buckets[mostPresentBucket] / (float)buckets.Sum(x => x.Value);
        }

        public readonly int Colors;
        public readonly Color Background;
        public readonly float BackgrounRatio;
        public readonly byte Difference;
    }
}

using AutoCrop.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace AutoCrop.Core.Models
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
                var buckets = colors.GroupBy(x => x.Key.ToColorBucket())
                                    .ToDictionary(x => x.Key, x => x.Sum(y => y.Value));

                var mostPresentColor = colors.OrderByDescending(x => x.Value)
                                             .First();

                var mostPresentBucket = mostPresentColor.Key.ToColorBucket();

                Background = mostPresentColor.Key;
                BucketRatio = buckets[mostPresentBucket] / (float)buckets.Sum(x => x.Value);
            }
        }

        public readonly int Colors;
        public readonly Color Background;
        public readonly float BucketRatio;
        public readonly bool Success;
        
    }
}

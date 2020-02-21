using ImageResizer.Plugins.AutoCrop.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class BorderAnalysis
    {
        public BorderAnalysis()
        {

        }

        public BorderAnalysis(IDictionary<Color, int> colors, IDictionary<int, int> buckets, int colorThreshold, float bucketThreshold)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));

            Colors = colors.Count;

            var mostPresentColor = colors.OrderByDescending(x => x.Value)
                                         .First();

            var mostPresentBucket = mostPresentColor.Key.ToColorBucket();

            Background = mostPresentColor.Key;
            BucketRatio = buckets[mostPresentBucket] / (float)buckets.Sum(x => x.Value);

            if (bucketThreshold >= 1.0f)
            {
                Success = colors.Count > 0 && colors.Count < colorThreshold;
            }
            else
            {
                Success = colors.Count > 0 && (colors.Count < colorThreshold || BucketRatio > bucketThreshold);
            }            
        }

        public readonly int Colors;
        public readonly Color Background;
        public readonly float BucketRatio;
        public readonly bool Success;
    }
}

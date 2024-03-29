﻿using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class CropAnalysis : ICropAnalysis
    {
        public Rectangle BoundingBox { get; set; }

        public Color Background { get; set; }

        public bool Success { get; set; }
    }
}

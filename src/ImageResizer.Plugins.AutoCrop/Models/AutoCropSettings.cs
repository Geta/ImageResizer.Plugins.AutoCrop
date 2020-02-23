namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class AutoCropSettings
    {
        public int PadX;
        public int PadY;
        public int Threshold = 35;
        public bool Parsed;
        public bool Debug;
        public bool SetMode;
        public FitMode Mode;
        public AutoCropMethod Method;
    }
}

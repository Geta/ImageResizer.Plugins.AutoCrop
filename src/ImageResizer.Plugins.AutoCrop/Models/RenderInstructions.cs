using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Models
{
    public class RenderInstructions
    {
        public Size Size;
        public Rectangle Source;
        public Rectangle Target;
        public Point Translate;
        public double Scale;
    }
}

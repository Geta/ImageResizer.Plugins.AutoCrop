using ImageResizer.Resizing;
using System.Drawing;

namespace ImageResizer.Plugins.AutoPad.Models
{
    public class AutoPadState
    {
        public AutoPadState(BoxPadding padding, Color backgroundColor)
        {
            Padding = padding;
            BackgroundColor = backgroundColor;
        }

        public readonly BoxPadding Padding;
        public readonly Color BackgroundColor;
    }
}

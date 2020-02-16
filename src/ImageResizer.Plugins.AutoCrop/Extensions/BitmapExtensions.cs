using ImageResizer.Plugins.AutoCrop.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class BitmapExtensions
    {
        const int pixelFormatIndexed = 0x00010000;
        const int pixelFormat32bppCMYK = 0x200F;
        const int pixelFormat16bppGrayScale = 4 | (16 << 8);

        /* Cred to ShadowChaser 
         * https://stackoverflow.com/questions/5065371/how-to-identify-cmyk-images-in-asp-net-using-c-sharp
         */

        public static ImageColorFormat GetColorFormat(this Bitmap bitmap)
        {
            var flags = (ImageFlags)bitmap.Flags;
            if (flags.HasFlag(ImageFlags.ColorSpaceCmyk) || flags.HasFlag(ImageFlags.ColorSpaceYcck))
            {
                return ImageColorFormat.Cmyk;
            }
            else if (flags.HasFlag(ImageFlags.ColorSpaceGray))
            {
                return ImageColorFormat.Grayscale;
            }

            var pixelFormat = (int)bitmap.PixelFormat;
            if (pixelFormat == pixelFormat32bppCMYK)
            {
                return ImageColorFormat.Cmyk;
            }
            else if ((pixelFormat & pixelFormatIndexed) != 0)
            {
                return ImageColorFormat.Indexed;
            }
            else if (pixelFormat == pixelFormat16bppGrayScale)
            {
                return ImageColorFormat.Grayscale;
            }

            return ImageColorFormat.Rgb;
        }
    }
}

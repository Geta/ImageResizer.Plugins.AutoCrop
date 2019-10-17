using System;
using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class RectangleExtensions
    {
        public static Rectangle ConstrainAspect(this Rectangle rectangle, int width, int height)
        {
            return ConstrainAspect(rectangle, width / (float)height, width, height);
        }

        public static Rectangle ConstrainAspect(this Rectangle rectangle, float aspect, int width, int height)
        {
            if (rectangle.Equals(Rectangle.Empty)) return rectangle;
            if (aspect == 0) return rectangle;

            var ta = rectangle.Width / (float)rectangle.Height;

            if (Math.Abs(aspect - ta) < 0.01f)
                return rectangle;

            if (aspect > ta)
            {
                var iw = rectangle.Height * aspect;
                var p = (int)Math.Ceiling((iw - rectangle.Width) * 0.5f);
                return Expand(rectangle, p, 0, width, height);
            }
            else
            {
                var ih = rectangle.Width / aspect;
                var p = (int)Math.Ceiling((ih - rectangle.Height) * 0.5f);
                return Expand(rectangle, 0, p, width, height);
            }
        }

        public static Rectangle Expand(this Rectangle rectangle, int paddingX, int paddingY, int width, int height)
        {
            if (paddingX == 0 && paddingY == 0)
                return rectangle;

            var xnc = 0;
            var xn = rectangle.X - paddingX;
            if (xn < 0)
            {
                xnc = -xn;
            }

            var xmc = 0;
            var xm = rectangle.Right + paddingX;
            if (xm > width)
            {
                xmc = xm - width;
            }

            var c = Math.Max(xnc, xmc);

            xn += c;
            xm -= c;

            var ync = 0;
            var yn = rectangle.Y - paddingY;
            if (yn < 0)
            {
                ync = -yn;
            }

            var ymc = 0;
            var ym = rectangle.Bottom + paddingY;
            if (ym > height)
            {
                ymc = ym - height;
            }

            c = Math.Max(ync, ymc);

            yn += c;
            ym -= c;

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }
    }
}

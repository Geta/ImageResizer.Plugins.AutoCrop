using System;
using System.Drawing;

namespace ImageResizer.Plugins.AutoCrop.Extensions
{
    public static class RectangleExtensions
    {
        public static Rectangle Aspect(this Rectangle rectangle, int width, int height)
        {
            return Aspect(rectangle, width / (float)height, width, height);
        }

        public static Rectangle Aspect(this Rectangle rectangle, float aspect, int width, int height)
        {
            if (rectangle.Equals(Rectangle.Empty)) return rectangle;
            if (aspect == 0) return rectangle;

            var ta = rectangle.Width / (float)rectangle.Height;

            if (Math.Abs(aspect - ta) < 0.01f)
                return rectangle;

            if (aspect > ta)
            {
                var iw = (int)Math.Ceiling(rectangle.Height * aspect);
                var p = (int)Math.Ceiling((iw - rectangle.Width) * 0.5f);
                return Expand(rectangle, p, 0, width, height);
            }
            else
            {
                var ih = (int)Math.Ceiling(rectangle.Width / aspect);
                var p = (int)Math.Ceiling((ih - rectangle.Height) * 0.5f);
                return Expand(rectangle, 0, p, width, height);
            }
        }

        public static Rectangle Aspect(this Rectangle rectangle, float aspect)
        {
            if (rectangle.Equals(Rectangle.Empty)) return rectangle;
            if (aspect == 0) return rectangle;

            var ta = rectangle.Width / (float)rectangle.Height;

            if (Math.Abs(aspect - ta) < 0.01f)
                return rectangle;

            if (aspect > ta)
            {
                var iw = (int)Math.Ceiling(rectangle.Height * aspect);
                var p = (int)Math.Ceiling((iw - rectangle.Width) * 0.5f);
                return Expand(rectangle, p, 0);
            }
            else
            {
                var ih = (int)Math.Ceiling(rectangle.Width / aspect);
                var p = (int)Math.Ceiling((ih - rectangle.Height) * 0.5f);
                return Expand(rectangle, 0, p);
            }
        }

        public static Rectangle Expand(this Rectangle rectangle, int paddingX, int paddingY)
        {
            if (paddingX == 0 && paddingY == 0) return rectangle;

            return new Rectangle(rectangle.X - paddingX, 
                                 rectangle.Y - paddingY, 
                                 rectangle.Width + paddingX * 2, 
                                 rectangle.Height + paddingY * 2);
        }

        public static Rectangle Expand(this Rectangle rectangle, int paddingX, int paddingY, int maxWidth, int maxHeight)
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
            if (xm > maxWidth)
            {
                xmc = xm - maxWidth;
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
            if (ym > maxHeight)
            {
                ymc = ym - maxHeight;
            }

            c = Math.Max(ync, ymc);

            yn += c;
            ym -= c;

            return new Rectangle(xn, yn, xm - xn, ym - yn);
        }
    }
}

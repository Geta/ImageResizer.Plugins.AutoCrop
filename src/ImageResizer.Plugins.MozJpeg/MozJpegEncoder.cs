using ImageResizer.Configuration;
using ImageResizer.Encoding;
using ImageResizer.Resizing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageResizer.Plugins.MozJpeg
{
    public class MozJpegEncoder : IPlugin, IEncoder
    {
        public bool SupportsTransparency => false;
        public string MimeType => "image/jpg";
        public string Extension => "jpg";
        public int Quality { get; set; }

        protected readonly ISet<string> _extensions;

        public MozJpegEncoder() : this(90) { }
        public MozJpegEncoder(int quality)
        {
            Quality = quality;

            _extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "jpg",
                "jpeg"
            };
        }

        public IEncoder CreateIfSuitable(ResizeSettings settings, object original)
        {
            var forcedFormat = !string.IsNullOrEmpty(settings.Format);
            var shouldEncode = forcedFormat ? _extensions.Contains(settings.Format) : _extensions.Contains(GetExtension(original));

            if (shouldEncode)
            {
                var quality = settings.Get<int>("quality");

                if (quality.HasValue)
                    return new MozJpegEncoder(quality.Value);

                return new MozJpegEncoder();
            }

            return null;
        }

        public string GetExtension(object original)
        {
            var bitmap = original as Bitmap;
            if (bitmap == null) 
                return null;

            var tag = bitmap.Tag as BitmapTag;
            if (tag == null)
                return null;

            var path = tag.Path;
            if (string.IsNullOrEmpty(path))
                return null;

            return Path.GetExtension(path)
                       .TrimStart('.');
        }

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public void Write(Image i, Stream s)
        {
            using (var bitmap = new Bitmap(i))
            using (var encoder = new MozJpeg())
            {
                var data = encoder.Encode(bitmap, Quality, false);
                s.Write(data, 0, data.Length);
            }
        }
    }
}

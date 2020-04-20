using ImageResizer.Configuration;
using ImageResizer.Plugins.AutoCrop.Actions;
using ImageResizer.Resizing;
using System.Collections.Generic;
using System.Drawing;

namespace ImageResizer.Plugins.AutoMatte
{
    public class AutoMattePlugin : BuilderExtension, IPlugin, IQuerystringPlugin
    {
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

        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[]
            {
                "autoMatte"
            };
        }
        protected override RequestedAction PreRenderImage(ImageState state)
        {
            if (state == null) return RequestedAction.None;
            if (state.settings == null) return RequestedAction.None;
            //if (state.preRenderBitmap == null)
                //state.preRenderBitmap = new Bitmap(state.sourceBitmap);

            //state.preRenderBitmap = Filter.Buckets(state.preRenderBitmap);

            return RequestedAction.None;
        }
    }
}

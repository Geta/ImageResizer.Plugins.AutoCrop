ImageResizer.Plugins.AutoCrop
=============================
Automatic cropping for images with a flat background.
Works with ImageResizer.NET 4.0.5 and above.

Uses a relative luminance tolerance bounding box to determine which area to crop.
Preserves original aspect ratio or image.

### Parameters

* **autoCrop** - activates plugin - values: '20' or '20|30' or '20|30|50' (x-padding, y-padding and tolerance, values separated by ,;|).
* **autoCropDebug** - displays determined bounding boxes without cropping - values: any.
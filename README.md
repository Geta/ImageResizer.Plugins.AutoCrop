# ImageResizer.Plugins.AutoCrop

* Master<br>
![](http://tc.geta.no/app/rest/builds/buildType:(id:GetaPackages_ImageResizerPluginsAutoCropAutomator_00ci),branch:master/statusIcon)

## Description

Automatic cropping for images with a flat background.
Works with ImageResizer.NET 4.0.5 and above.

Uses either a relative luminance tolerance or an edge detection filter to determine which area to crop.
Preserves intended aspect ratio of image.

## Features

- Crops images with a flat background
- Configurable x and y padding
- Configurable treshold
- Configurable analysis method (color difference or edge detection)
- Can override FitMode of regular resizer

## How to get started?

Requires ImageResizer 4.0.5 or above

- `install-package ImageResizer.Plugins.AutoCrop`

Will add the following to `web.config`

```
<configuration>
    <resizer>
        <plugins>
            <add name="AutoCrop" />
        </plugins>
    </resizer>    
</configuration>
```

## Details

Plugin unlocks the ability to use the following query parameters for images

| Parameter | Description | Example |
| --------- | ----------- | ------- |
| autoCrop | activates the plugin with the provided values | _?autoCrop=10_ or _?autoCrop=10;20;30_ |
| _x-padding_ | the first provided parameter value | 10 |
| _y-padding_ | the second provided parameter value | 10;_20_ |
| _threshold_ | the third provided parameter value, background color deviation threshold | 10;20;_30_ |
| autoCropMode (optional) | overrides the fit mode if the autoCrop is successfully completed | _?autoCropMode=pad_ |
| autoCropMethod (optional) | determines which method to use (tolerance or edge) | _?autoCropMethod=tolerance_ |
| autoCropDebug (optional) | displays a debug visualisation of how the plugin evaluated instead of cropping | _?autoCropDebug=1_ |

## Package maintainer

https://github.com/svenrog

## Changelog

[Changelog](CHANGELOG.md)

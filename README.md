# ImageResizer.Plugins.AutoCrop

## Description

Automatic cropping for images with a flat background.
Works with ImageResizer.NET 4.0.5 and above.

Uses a relative luminance tolerance bounding box to determine which area to crop.
Preserves original aspect ratio or image.

## Features

- Crops images with a flat background
- Configurable x and y padding
- Configurable color difference treshold

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
| autoCropDebug | displays a debug visualisation of how the plugin evaluated instead of cropping | _?autoCropDebug=1_ |

## Package maintainer

https://github.com/svenrog

## Changelog

[Changelog](CHANGELOG.md)
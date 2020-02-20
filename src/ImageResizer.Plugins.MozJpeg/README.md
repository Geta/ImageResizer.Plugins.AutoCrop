# ImageResizer.Plugins.MozJpeg

## Description

Automatic size reduction for jpg images.
Works with ImageResizer.NET 4.0.5 and above.

Based on [Mozilla JPEG Encoder](https://github.com/mozilla/mozjpeg#mozilla-jpeg-encoder-project).
MozJPEG reduces file sizes of JPEG images while retaining quality and compatibility with the vast majority of the world's deployed decoders.

## Features

- Encodes jpg images based on file extension.
- Encodes jpg images based on &format-setting.

## How to get started?

Requires ImageResizer 4.0.5 or above

- `install-package ImageResizer.Plugins.MozJpeg`

Will add the following to `web.config`

```
<configuration>
    <resizer>
        <plugins>
            <add name="MozJpeg" />
        </plugins>
    </resizer>    
</configuration>
```

## Details

Everything should be set up by just installing the nuget.
If you want to force usage of this encoder, supply `&format=jpg` to the query string.

## Thanks to

Jose M. Piñeiro for providing a C# wrapper for the encoder.
https://github.com/JosePineiro/MozJpeg-wrapper

## Package maintainer

https://github.com/svenrog

## Changelog

[Changelog](CHANGELOG.md)
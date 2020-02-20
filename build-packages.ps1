cd .\.nuget

.\nuget.exe pack ..\src\ImageResizer.Plugins.AutoCrop\ImageResizer.Plugins.AutoCrop.csproj -Properties Configuration=Release
.\nuget.exe pack ..\src\ImageResizer.Plugins.MozJpeg\ImageResizer.Plugins.MozJpeg.csproj -Properties Configuration=Release
cd ..\
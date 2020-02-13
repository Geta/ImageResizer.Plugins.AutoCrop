namespace ImageResizer.Plugins.AutoCrop.Analyzers
{
    public interface IBorderAnalyzer
    {
        bool DirtyBorder { get; set; }
    }
}

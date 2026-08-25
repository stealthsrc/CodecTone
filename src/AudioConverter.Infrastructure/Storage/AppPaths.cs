namespace AudioConverter.Infrastructure.Storage;

public static class AppPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioConverter");

    public static string PreviewWave => Path.Combine(Root, "preview", "selection.wav");
    public static string RemixPreviewWave => Path.Combine(Root, "remix", "preview.wav");
    public static string RemixStaging => Path.Combine(Root, "remix", "staging");
}

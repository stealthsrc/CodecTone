namespace AudioConverter.Infrastructure.Storage;

public static class AppPaths
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioConverter");

    public static string PreviewWave => Path.Combine(Root, "preview", "selection.wav");
}

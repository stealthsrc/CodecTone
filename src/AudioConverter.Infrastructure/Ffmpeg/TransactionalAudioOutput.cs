namespace AudioConverter.Infrastructure.Ffmpeg;

public static class TransactionalAudioOutput
{
    public static async Task RunAsync(
        string inputPath,
        string destinationPath,
        bool overwrite,
        Func<string, CancellationToken, Task> produce,
        Func<string, CancellationToken, Task> validate,
        CancellationToken cancellationToken = default)
    {
        var input = Path.GetFullPath(inputPath);
        var destination = Path.GetFullPath(destinationPath);
        if (input.Equals(destination, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Source and destination must be different files.");
        if (File.Exists(destination) && !overwrite)
            throw new IOException($"Destination already exists: {destination}");

        var directory = Path.GetDirectoryName(destination)!;
        Directory.CreateDirectory(directory);
        var extension = Path.GetExtension(destination);
        var staged = Path.Combine(directory, $".{Path.GetFileNameWithoutExtension(destination)}.{Guid.NewGuid():N}.tmp{extension}");
        try
        {
            await produce(staged, cancellationToken);
            await validate(staged, cancellationToken);
            File.Move(staged, destination, overwrite);
        }
        finally
        {
            try { File.Delete(staged); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

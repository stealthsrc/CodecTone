using System.Text;
namespace AudioConverter.Infrastructure.Storage;

public sealed class LocalDiagnosticLog(string? directory = null)
{
    private static readonly object Gate = new();
    public string FilePath { get; } = Path.Combine(directory ?? Path.Combine(AppPaths.Root, "diagnostics"), "diagnostic.log");
    public void Write(string operation, string message)
    {
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length >= 512 * 1024)
                    File.Move(FilePath, FilePath + ".previous", true);
                var clean = message.Replace('\r', ' ').Replace('\n', ' ');
                if (clean.Length > 4000) clean = clean[..4000];
                File.AppendAllText(FilePath, $"{DateTimeOffset.Now:O} [{operation}] {clean}{Environment.NewLine}", Encoding.UTF8);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class TransactionalAudioOutputTests
{
    [TestMethod]
    public async Task RunAsync_PreservesExistingDestinationWhenEncodingFails()
    {
        using var directory = new TemporaryDirectory();
        var input = directory.File("input.flac", "source");
        var output = directory.File("output.mp3", "healthy output");

        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => TransactionalAudioOutput.RunAsync(
            input,
            output,
            overwrite: true,
            async (staged, _) =>
            {
                await File.WriteAllTextAsync(staged, "partial");
                throw new InvalidDataException("encoding failed");
            },
            (_, _) => Task.CompletedTask));

        Assert.AreEqual("healthy output", await File.ReadAllTextAsync(output));
        Assert.AreEqual(2, Directory.GetFiles(directory.Path).Length);
    }

    [TestMethod]
    public async Task RunAsync_RejectsCollisionBeforeStartingEncoder()
    {
        using var directory = new TemporaryDirectory();
        var input = directory.File("input.flac", "source");
        var output = directory.File("output.mp3", "existing");
        var encoderCalled = false;

        await Assert.ThrowsExceptionAsync<IOException>(() => TransactionalAudioOutput.RunAsync(
            input,
            output,
            overwrite: false,
            (_, _) => { encoderCalled = true; return Task.CompletedTask; },
            (_, _) => Task.CompletedTask));

        Assert.IsFalse(encoderCalled);
    }

    [TestMethod]
    public async Task RunAsync_ValidatesThenAtomicallyReplacesDestination()
    {
        using var directory = new TemporaryDirectory();
        var input = directory.File("input.flac", "source");
        var output = directory.File("output.mp3", "old");
        var validated = false;

        await TransactionalAudioOutput.RunAsync(
            input,
            output,
            overwrite: true,
            (staged, token) => File.WriteAllTextAsync(staged, "new", token),
            async (staged, token) => { validated = await File.ReadAllTextAsync(staged, token) == "new"; });

        Assert.IsTrue(validated);
        Assert.AreEqual("new", await File.ReadAllTextAsync(output));
    }

    [TestMethod]
    public async Task RunAsync_NeverAllowsSourceAndDestinationToMatch()
    {
        using var directory = new TemporaryDirectory();
        var input = directory.File("song.flac", "source");

        await Assert.ThrowsExceptionAsync<IOException>(() => TransactionalAudioOutput.RunAsync(
            input, input, overwrite: true, (_, _) => Task.CompletedTask, (_, _) => Task.CompletedTask));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codectone-output-{Guid.NewGuid():N}");
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public string File(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllText(path, content);
            return path;
        }
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}

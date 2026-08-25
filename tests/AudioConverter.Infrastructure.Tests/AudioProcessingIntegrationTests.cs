using System.Security.Cryptography;
using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class AudioProcessingIntegrationTests
{
    [TestMethod]
    [TestCategory("RequiresFfmpeg")]
    public async Task ConvertAsync_FailedOverwritePreservesValidatedDestination()
    {
        FfmpegTools tools;
        try { tools = FfmpegLocator.Find(); }
        catch (FfmpegDependencyException) { Assert.Inconclusive("FFmpeg is not available for integration testing."); return; }

        var root = Path.Combine(Path.GetTempPath(), $"codectone-integration-{Guid.NewGuid():N}");
        var goodDirectory = Path.Combine(root, "good");
        var badDirectory = Path.Combine(root, "bad");
        var outputDirectory = Path.Combine(root, "output");
        Directory.CreateDirectory(goodDirectory);
        Directory.CreateDirectory(badDirectory);
        Directory.CreateDirectory(outputDirectory);
        try
        {
            var goodInput = Path.Combine(goodDirectory, "song.wav");
            var badInput = Path.Combine(badDirectory, "song.wav");
            var output = Path.Combine(outputDirectory, "song.mp3");
            await new FfmpegProcessRunner().RunAsync(
                tools.FfmpegPath,
                ["-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=1", "-c:a", "pcm_s16le", goodInput],
                1);
            await File.WriteAllBytesAsync(badInput, [0, 1, 2, 3]);

            var service = new AudioProcessingService(tools);
            await service.ConvertAsync(goodInput, output, new ConversionOptions(AudioFormat.Mp3, "192k", Overwrite: true), 1);
            var expectedHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output)));

            await Assert.ThrowsExceptionAsync<FfmpegExecutionException>(() => service.ConvertAsync(
                badInput, output, new ConversionOptions(AudioFormat.Mp3, "192k", Overwrite: true), 1));

            Assert.AreEqual(expectedHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(output))));
            Assert.AreEqual(0, Directory.GetFiles(outputDirectory, "*.tmp*", SearchOption.TopDirectoryOnly).Length);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

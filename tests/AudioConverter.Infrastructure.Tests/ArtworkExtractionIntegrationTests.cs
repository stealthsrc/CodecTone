using AudioConverter.Core.Artwork;
using AudioConverter.Infrastructure.Artwork;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class ArtworkExtractionIntegrationTests
{
    [TestMethod]
    [TestCategory("RequiresFfmpeg")]
    public async Task ExtractAsync_ConvertsValidatesAndSkipsIdenticalSecondRun()
    {
        FfmpegTools tools;
        try { tools = FfmpegLocator.Find(); }
        catch (FfmpegDependencyException) { Assert.Inconclusive("FFmpeg is not available for integration testing."); return; }

        var root = Path.Combine(Path.GetTempPath(), $"codectone-artwork-integration-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var cover = Path.Combine(root, "cover.png");
            var audio = Path.Combine(root, "audio.flac");
            var song = Path.Combine(root, "song.flac");
            var output = Path.Combine(root, "output");
            var runner = new FfmpegProcessRunner();
            await runner.RunAsync(tools.FfmpegPath, ["-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "color=c=green:s=640x640:d=1", "-frames:v", "1", cover], 1);
            await runner.RunAsync(tools.FfmpegPath, ["-hide_banner", "-loglevel", "error", "-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=1", "-c:a", "flac", audio], 1);
            await runner.RunAsync(tools.FfmpegPath,
                ["-hide_banner", "-loglevel", "error", "-y", "-i", audio, "-i", cover, "-map", "0:a", "-map", "1:v", "-c", "copy", "-disposition:v:0", "attached_pic", "-metadata", "artist=Artist", "-metadata", "album=Album", "-metadata:s:v:0", "comment=Cover (front)", song], 1);

            var probe = new FfprobeService(tools.FfprobePath);
            var info = await probe.ProbeAsync(song);
            var album = ArtworkPlanner.Create([new ArtworkSource(song, info.Tags, info.ArtworkStreams)]).Albums.Single();
            var service = new ArtworkExtractionService(tools);
            var options = new ArtworkExtractionOptions(ArtworkOutputFormat.Png, 120);

            var first = await service.ExtractAsync(album, output, options);
            var second = await service.ExtractAsync(album, output, options);
            var broken = album with { SourcePath = Path.Combine(root, "missing.flac"), Fallbacks = [album] };
            var recovered = await service.ExtractAsync(broken, Path.Combine(root, "fallback"), options);
            Assert.IsNull(recovered.Error);
            Assert.AreEqual(song, recovered.Album.SourcePath);

            Assert.IsNull(first.Error);
            Assert.IsFalse(first.Skipped);
            Assert.IsTrue(second.Skipped);
            var image = await probe.ProbeImageAsync(first.OutputPath!);
            Assert.AreEqual(120, image.Width);
            Assert.AreEqual(120, image.Height);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

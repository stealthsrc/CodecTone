using AudioConverter.Core.Artwork;
using AudioConverter.Infrastructure.Artwork;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class ArtworkCommandBuilderTests
{
    private static readonly EmbeddedArtworkInfo Artwork = new(2, "mjpeg", 1600, 1600, true, true);

    [TestMethod]
    public void Build_OriginalCopiesSelectedImageStream()
    {
        var args = ArtworkCommandBuilder.Build("song.flac", "cover.jpg", Artwork, new ArtworkExtractionOptions(ArtworkOutputFormat.Original));

        AssertArgumentPair(args, "-map", "0:2");
        AssertArgumentPair(args, "-c:v", "copy");
        Assert.AreEqual("cover.jpg", args[^1]);
    }

    [TestMethod]
    public void Build_PngConvertsOneFrameWithoutUpscalingBeyondMaximum()
    {
        var args = ArtworkCommandBuilder.Build("song.flac", "cover.png", Artwork, new ArtworkExtractionOptions(ArtworkOutputFormat.Png, 1200));

        AssertArgumentPair(args, "-c:v", "png");
        CollectionAssert.Contains(args.ToList(), "scale=w='min(1200,iw)':h='min(1200,ih)':force_original_aspect_ratio=decrease");
        AssertArgumentPair(args, "-frames:v", "1");
    }

    [DataTestMethod]
    [DataRow(ArtworkOutputFormat.Jpeg, "mjpeg")]
    [DataRow(ArtworkOutputFormat.Webp, "libwebp")]
    public void Build_UsesRequestedLossyImageEncoder(ArtworkOutputFormat format, string codec)
    {
        var args = ArtworkCommandBuilder.Build("song.flac", "cover.out", Artwork, new ArtworkExtractionOptions(format));

        AssertArgumentPair(args, "-c:v", codec);
    }

    private static void AssertArgumentPair(IReadOnlyList<string> args, string key, string value)
    {
        var index = args.ToList().IndexOf(key);
        Assert.IsTrue(index >= 0, $"Missing argument: {key}");
        Assert.AreEqual(value, args[index + 1]);
    }
}

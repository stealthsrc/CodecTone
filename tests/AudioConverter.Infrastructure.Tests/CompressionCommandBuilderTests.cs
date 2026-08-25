using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class CompressionCommandBuilderTests
{
    [TestMethod]
    public void BuildCompression_UsesMp3VbrProfile()
    {
        var options = new CompressionOptions(AudioFormat.Mp3, CompressionProfile.HighFidelity);

        var args = FfmpegCommandBuilder.BuildCompression("in.flac", "out.mp3", options, null, true);

        AssertArgumentPair(args, "-c:a", "libmp3lame");
        AssertArgumentPair(args, "-q:a", "2");
        CollectionAssert.DoesNotContain(args.ToList(), "-b:a");
    }

    [TestMethod]
    public void BuildCompression_UsesTargetAbr()
    {
        var options = new CompressionOptions(
            AudioFormat.M4a,
            CompressionProfile.TargetTotalSize,
            TargetTotalBytes: 5_000_000);

        var args = FfmpegCommandBuilder.BuildCompression("in.wav", "out.m4a", options, 144, false);

        AssertArgumentPair(args, "-c:a", "aac");
        AssertArgumentPair(args, "-b:a", "144k");
    }

    [TestMethod]
    public void BuildCompression_UsesMaximumFlacLevel()
    {
        var options = new CompressionOptions(AudioFormat.Flac, CompressionProfile.MaximumReduction);

        var args = FfmpegCommandBuilder.BuildCompression("in.wav", "out.flac", options, null, false);

        AssertArgumentPair(args, "-c:a", "flac");
        AssertArgumentPair(args, "-compression_level", "12");
    }

    [TestMethod]
    public void BuildCompression_OptimizesMappedArtwork()
    {
        var options = new CompressionOptions(
            AudioFormat.Mp3,
            CompressionProfile.Balanced,
            OptimizeArtwork: true,
            PreserveMetadata: true);

        var args = FfmpegCommandBuilder.BuildCompression("in.flac", "out.mp3", options, null, false);

        CollectionAssert.Contains(args.ToList(), "0:v:disp:attached_pic?");
        AssertArgumentPair(args, "-vf", "scale=1200:1200:force_original_aspect_ratio=decrease");
        AssertArgumentPair(args, "-q:v", "3");
    }

    [TestMethod]
    public void BuildCompression_DoesNotCreateVideoFilterWithoutArtwork()
    {
        var options = new CompressionOptions(
            AudioFormat.Mp3,
            CompressionProfile.HighFidelity,
            OptimizeArtwork: true,
            PreserveMetadata: true);

        var args = FfmpegCommandBuilder.BuildCompression(
            "in.wav", "out.mp3", options, null, false, hasCoverArt: false);

        CollectionAssert.DoesNotContain(args.ToList(), "-vf");
        CollectionAssert.DoesNotContain(args.ToList(), "-c:v");
    }

    private static void AssertArgumentPair(string[] args, string name, string expected)
    {
        var index = Array.IndexOf(args, name);
        Assert.IsTrue(index >= 0, $"Missing {name}");
        Assert.AreEqual(expected, args[index + 1]);
    }
}

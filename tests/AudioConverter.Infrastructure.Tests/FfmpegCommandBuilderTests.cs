using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfmpegCommandBuilderTests
{
    [TestMethod]
    public void BuildConversion_PreservesMetadataAndArtworkForMp3()
    {
        var options = new ConversionOptions(AudioFormat.Mp3, "320k", PreserveMetadata: true);

        var args = FfmpegCommandBuilder.BuildConversion("in.flac", "out.mp3", options, true);

        CollectionAssert.Contains(args.ToList(), "-map_metadata");
        CollectionAssert.Contains(args.ToList(), "0:v:disp:attached_pic?");
        CollectionAssert.Contains(args.ToList(), "libmp3lame");
        CollectionAssert.Contains(args.ToList(), "320k");
        CollectionAssert.Contains(args.ToList(), "mjpeg");
        CollectionAssert.Contains(args.ToList(), "pipe:1");
    }

    [TestMethod]
    public void BuildConversion_RemovesMetadataAndVideoWhenDisabled()
    {
        var options = new ConversionOptions(AudioFormat.Ogg, "192k", PreserveMetadata: false);

        var args = FfmpegCommandBuilder.BuildConversion("in.wav", "out.ogg", options, false);

        CollectionAssert.Contains(args.ToList(), "-vn");
        CollectionAssert.Contains(args.ToList(), "-1");
        CollectionAssert.DoesNotContain(args.ToList(), "attached_pic");
    }

    [TestMethod]
    public void BuildConversion_UsesRequestedWavDepthAndRate()
    {
        var options = new ConversionOptions(AudioFormat.Wav, SampleRate: 48000, BitDepth: 24);

        var args = FfmpegCommandBuilder.BuildConversion("in.flac", "out.wav", options, false);

        CollectionAssert.Contains(args.ToList(), "pcm_s24le");
        CollectionAssert.Contains(args.ToList(), "48000");
    }

    [TestMethod]
    public void BuildTrim_AddsAccurateTrimAndFadeFilters()
    {
        var options = new ConversionOptions(AudioFormat.Flac);
        var trim = TrimSelection.Create(2.5, 12.5, 1, 2);

        var args = FfmpegCommandBuilder.BuildTrim("in.flac", "out.flac", options, trim, true);
        var filter = args[Array.IndexOf(args, "-af") + 1];

        Assert.AreEqual(
            "atrim=start=2.500:end=12.500,asetpts=PTS-STARTPTS,afade=t=in:st=0:d=1.000,afade=t=out:st=8.000:d=2.000",
            filter);
    }

    [TestMethod]
    public void BuildPreview_RendersLocalPcmWave()
    {
        var args = FfmpegCommandBuilder.BuildPreview(
            "in.m4a", "preview.wav", TrimSelection.Create(0, 5), true);

        CollectionAssert.Contains(args.ToList(), "pcm_s16le");
        CollectionAssert.Contains(args.ToList(), "44100");
        Assert.AreEqual("preview.wav", args[^1]);
    }

    [TestMethod]
    public void BuildWaveform_BoundsDecodedSamplesForVeryLongAudio()
    {
        var args = FfmpegCommandBuilder.BuildWaveform("long.flac", width: 900, durationSeconds: 36_000);

        CollectionAssert.DoesNotContain(args, "-ar");
        CollectionAssert.DoesNotContain(args, "-ac");
    }
}

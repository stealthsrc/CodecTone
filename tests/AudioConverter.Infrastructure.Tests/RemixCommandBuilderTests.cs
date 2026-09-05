using AudioConverter.Core.Models;
using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Remix;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class RemixCommandBuilderTests
{
    [TestMethod]
    public void BuildExport_AppliesStandardAndCustomMetadata()
    {
        var metadata = new RemixMetadata(
            Title: "New title",
            Artist: "New artist",
            CustomTags: new Dictionary<string, string> { ["remix.by"] = "CodecTone" });
        var encoding = new ConversionOptions(AudioFormat.Mp3, "320k", PreserveMetadata: true);

        var args = RemixCommandBuilder.BuildExport(
            "in.flac", "out.mp3", [], encoding, metadata, 44_100, 120, sourceHasCover: false);

        CollectionAssert.Contains(args.ToList(), "title=New title");
        CollectionAssert.Contains(args.ToList(), "artist=New artist");
        CollectionAssert.Contains(args.ToList(), "remix.by=CodecTone");
    }

    [TestMethod]
    public void BuildExport_ReplacesCoverFromSecondInput()
    {
        var metadata = new RemixMetadata(CoverAction: CoverArtAction.Replace, CoverPath: "cover.png");
        var encoding = new ConversionOptions(AudioFormat.M4a, "192k");

        var args = RemixCommandBuilder.BuildExport(
            "in.flac", "out.m4a", [], encoding, metadata, 44_100, 120, sourceHasCover: true);

        AssertArgumentPair(args, "-i", "cover.png", occurrence: 2);
        CollectionAssert.Contains(args.ToList(), "1:v:0");
        CollectionAssert.Contains(args.ToList(), "attached_pic");
    }

    [TestMethod]
    public void BuildExport_RemovesCoverWhenRequested()
    {
        var metadata = new RemixMetadata(CoverAction: CoverArtAction.Remove);
        var encoding = new ConversionOptions(AudioFormat.Flac);

        var args = RemixCommandBuilder.BuildExport(
            "in.flac", "out.flac", [], encoding, metadata, 44_100, 120, sourceHasCover: true);

        CollectionAssert.Contains(args.ToList(), "-vn");
        CollectionAssert.DoesNotContain(args.ToList(), "0:v:disp:attached_pic?");
    }

    [TestMethod]
    public void BuildPreview_TrimsTwentySecondsAndRendersWave()
    {
        var args = RemixCommandBuilder.BuildPreview(
            "in.flac", "preview.wav", [new BassEffect(8, 90)], 44_100, 120, 25, 20);

        CollectionAssert.Contains(args.ToList(), "pcm_s16le");
        var filter = args[Array.IndexOf(args, "-af") + 1];
        StringAssert.EndsWith(filter, "atrim=start=25:end=45,asetpts=PTS-STARTPTS");
    }

    [TestMethod]
    public void BuildPreview_AddsImpulseResponseForNaturalReverb()
    {
        var args = RemixCommandBuilder.BuildPreview(
            "in.flac",
            "preview.wav",
            [new TempoPitchEffect(0.85), new ReverbEffect(0.28, 2.4)],
            44_100,
            120,
            5,
            20,
            "hall-ir.wav");

        AssertArgumentPair(args, "-i", "hall-ir.wav", occurrence: 2);
        CollectionAssert.Contains(args.ToList(), "-filter_complex");
        CollectionAssert.Contains(args.ToList(), "[previewout]");
    }

    private static void AssertArgumentPair(string[] args, string name, string expected, int occurrence)
    {
        var found = 0;
        for (var index = 0; index < args.Length - 1; index++)
        {
            if (args[index] != name) continue;
            found++;
            if (found == occurrence) { Assert.AreEqual(expected, args[index + 1]); return; }
        }
        Assert.Fail($"Missing occurrence {occurrence} of {name}");
    }
}

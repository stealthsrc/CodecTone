using AudioConverter.Core.Compression;
using AudioConverter.Core.Models;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class CompressionPlannerTests
{
    [DataTestMethod]
    [DataRow(AudioFormat.Mp3, CompressionProfile.HighFidelity, 190)]
    [DataRow(AudioFormat.Ogg, CompressionProfile.Balanced, 192)]
    [DataRow(AudioFormat.M4a, CompressionProfile.MaximumReduction, 128)]
    public void NominalBitrate_ReturnsProfileMapping(AudioFormat format, CompressionProfile profile, int expected)
    {
        Assert.AreEqual(expected, CompressionProfiles.NominalBitrateKbps(format, profile));
    }

    [DataTestMethod]
    [DataRow(CompressionProfile.HighFidelity, 8)]
    [DataRow(CompressionProfile.Balanced, 10)]
    [DataRow(CompressionProfile.MaximumReduction, 12)]
    public void FlacLevel_ReturnsLosslessProfileMapping(CompressionProfile profile, int expected)
    {
        Assert.AreEqual(expected, CompressionProfiles.FlacLevel(profile));
    }

    [TestMethod]
    public void Create_TargetBudgetAllocatesOneBitrateByTotalDuration()
    {
        var sources = new[]
        {
            Source("one.wav", AudioFormat.Wav, 60, 10_000_000),
            Source("two.flac", AudioFormat.Flac, 60, 6_000_000),
        };
        var options = new CompressionOptions(AudioFormat.Mp3, CompressionProfile.TargetTotalSize, 3_000_000);

        var plan = CompressionPlanner.Create(sources, options);

        Assert.AreEqual(195, plan.TargetAudioBitrateKbps);
        Assert.AreEqual(1_025_536, plan.MinimumTargetBytes);
        Assert.AreEqual(3_000_000, plan.EstimatedOutputBytes);
    }

    [TestMethod]
    public void Create_RejectsTargetBelowCodecMinimum()
    {
        var options = new CompressionOptions(AudioFormat.Mp3, CompressionProfile.TargetTotalSize, 500_000);

        var error = Assert.ThrowsException<ArgumentException>(() =>
            CompressionPlanner.Create([Source("one.wav", AudioFormat.Wav, 120, 20_000_000)], options));

        StringAssert.Contains(error.Message, "minimum");
    }

    [TestMethod]
    public void Create_RejectsTargetSizeForFlac()
    {
        var options = new CompressionOptions(AudioFormat.Flac, CompressionProfile.TargetTotalSize, 5_000_000);

        Assert.ThrowsException<ArgumentException>(() =>
            CompressionPlanner.Create([Source("one.wav", AudioFormat.Wav, 60, 10_000_000)], options));
    }

    [TestMethod]
    public void Create_SkipsFileWhenNoReductionIsExpected()
    {
        var options = new CompressionOptions(AudioFormat.Mp3, CompressionProfile.HighFidelity);

        var plan = CompressionPlanner.Create([Source("small.mp3", AudioFormat.Mp3, 60, 1_000_000)], options);

        Assert.IsTrue(plan.Files[0].ShouldSkip);
        StringAssert.Contains(plan.Files[0].SkipReason!, "reduction");
    }

    private static CompressionSource Source(
        string path,
        AudioFormat format,
        double duration,
        long bytes,
        bool cover = false) =>
        new(path, Path.GetFileName(path), format, duration, bytes, null, cover);
}

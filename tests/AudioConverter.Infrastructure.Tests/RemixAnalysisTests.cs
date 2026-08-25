using AudioConverter.Core.Remix;
using AudioConverter.Infrastructure.Remix;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class RemixAnalysisTests
{
    [TestMethod]
    public void ParseDynamics_DerivesCrestFactorFromPeakAndRms()
    {
        const string output = """
        [Parsed_astats_0] Peak level dB: -1.25
        [Parsed_astats_0] RMS level dB: -13.75
        """;

        var result = RemixAnalysisParser.ParseDynamics(output);

        Assert.AreEqual(-1.25, result.PeakDb, 0.001);
        Assert.AreEqual(12.5, result.CrestFactorDb, 0.001);
    }

    [TestMethod]
    public void ParseDynamics_TreatsSilenceAsZeroCrest()
    {
        const string output = """
        [Parsed_astats_0] Peak level dB: -inf
        [Parsed_astats_0] RMS level dB: -inf
        """;

        var result = RemixAnalysisParser.ParseDynamics(output);

        Assert.AreEqual(0, result.CrestFactorDb);
    }

    [TestMethod]
    public void ParseSpectral_ReadsThreeBandsAndCalculatesStereoWidth()
    {
        const string output = """
        [Parsed_astats_2 @ 000001] RMS level dB: -9
        [Parsed_astats_4 @ 000002] RMS level dB: -15
        [Parsed_astats_6 @ 000003] RMS level dB: -20
        [Parsed_astats_8 @ 000004] RMS level dB: -12
        [Parsed_astats_10 @ 000005] RMS level dB: -18
        """;

        var result = RemixAnalysisParser.ParseSpectral(output);

        Assert.AreEqual(-9, result.LowEnergyDb);
        Assert.AreEqual(-15, result.MidEnergyDb);
        Assert.AreEqual(-20, result.HighEnergyDb);
        Assert.AreEqual(0.501, result.StereoWidth, 0.002);
    }

    [TestMethod]
    public async Task Cache_InvalidatesEntryWhenSourceChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codectone-analysis-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "song.wav");
        await File.WriteAllTextAsync(source, "first");
        var cache = new RemixAnalysisCache(Path.Combine(root, "cache"));
        var analysis = new AudioAnalysis(-14, -1, 7, 10, -18, -16, -20, 0.5, 180, 44_100, 2);
        try
        {
            await cache.WriteAsync(source, analysis);
            Assert.AreEqual(analysis, await cache.TryReadAsync(source));

            await File.AppendAllTextAsync(source, "changed");
            Assert.IsNull(await cache.TryReadAsync(source));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ParseSpectral_TreatsSilentSideChannelAsMonoWidth()
    {
        const string output = """
        [Parsed_astats_2 @ 1] RMS level dB: -12
        [Parsed_astats_4 @ 2] RMS level dB: -14
        [Parsed_astats_6 @ 3] RMS level dB: -20
        [Parsed_astats_8 @ 4] RMS level dB: -10
        [Parsed_astats_10 @ 5] RMS level dB: -inf
        """;

        Assert.AreEqual(0, RemixAnalysisParser.ParseSpectral(output).StereoWidth);
    }

    [TestMethod]
    public async Task Cache_SerializesConcurrentWritesForSameSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"codectone-analysis-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "song.wav");
        await File.WriteAllTextAsync(source, "source");
        var cache = new RemixAnalysisCache(Path.Combine(root, "cache"));
        var analysis = new AudioAnalysis(-14, -1, 7, 10, -18, -16, -20, 0.5, 180, 44_100, 2);
        try
        {
            await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => cache.WriteAsync(source, analysis)));
            Assert.AreEqual(analysis, await cache.TryReadAsync(source));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

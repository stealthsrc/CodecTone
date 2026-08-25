using AudioConverter.Core.Remix;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class AdaptiveRemixPresetTests
{
    [TestMethod]
    public void Catalog_GroupsEveryPresetIntoExpectedCategories()
    {
        var presets = RemixPresetCatalog.All;

        Assert.AreEqual(32, presets.Count);
        CollectionAssert.AreEquivalent(
            Enum.GetValues<RemixPresetCategory>(),
            presets.Select(item => item.Category).Distinct().ToArray());
        Assert.AreEqual(RemixPresetCategory.SpeedPitch, presets.Single(item => item.Preset == RemixPreset.SlowedReverb).Category);
        Assert.AreEqual(RemixPresetCategory.Mastering, presets.Single(item => item.Preset == RemixPreset.StreamingMaster).Category);
        foreach (var preset in presets)
        foreach (var intensity in Enum.GetValues<RemixIntensity>())
        {
            var rack = RemixPresetFactory.CreateAdaptive(preset.Preset, intensity, Analysis()).Rack;
            RemixRackValidator.Validate(rack, 180);
        }
    }

    [TestMethod]
    public void CreateAdaptive_LightIntensityMovesCreativeValuesTowardNeutral()
    {
        var medium = RemixPresetFactory.CreateAdaptive(RemixPreset.SlowedReverb, RemixIntensity.Medium, null);
        var light = RemixPresetFactory.CreateAdaptive(RemixPreset.SlowedReverb, RemixIntensity.Light, null);

        Assert.IsTrue(light.Rack.OfType<TempoPitchEffect>().Single().Rate > medium.Rack.OfType<TempoPitchEffect>().Single().Rate);
        Assert.IsTrue(light.Rack.OfType<ReverbEffect>().Single().Mix < medium.Rack.OfType<ReverbEffect>().Single().Mix);
        Assert.IsFalse(light.IsAdaptive);
        StringAssert.Contains(light.Explanation, "static defaults");
    }

    [TestMethod]
    public void CreateAdaptive_ReducesBassBoostForBassHeavySong()
    {
        var analysis = Analysis(lowDb: -9, midDb: -16, highDb: -21);

        var result = RemixPresetFactory.CreateAdaptive(RemixPreset.BassBoost, RemixIntensity.Strong, analysis);

        var bass = result.Rack.OfType<BassEffect>().Single();
        Assert.IsTrue(bass.GainDb < 10);
        Assert.IsTrue(result.IsAdaptive);
        StringAssert.Contains(result.Explanation, "bass-heavy");
        RemixRackValidator.Validate(result.Rack, analysis.DurationSeconds);
    }

    [TestMethod]
    public void CreateAdaptive_AvoidsOverWideningAlreadyWideSong()
    {
        var analysis = Analysis(stereoWidth: 0.92);

        var result = RemixPresetFactory.CreateAdaptive(RemixPreset.WideMaster, RemixIntensity.Strong, analysis);

        Assert.IsTrue(result.Rack.OfType<StereoWidthEffect>().Single().Width <= 1.1);
        StringAssert.Contains(result.Explanation, "already wide");
    }

    [TestMethod]
    public void NewMasteringEffects_CreateValidFilterableRack()
    {
        RemixEffect[] rack =
        [
            new HighPassEffect(35),
            new LowPassEffect(18_000),
            new CompressorEffect(-18, 3, 2),
            new StereoWidthEffect(1.15),
            new SoftLimiterEffect(-1),
        ];

        RemixRackValidator.Validate(rack, 180);
    }

    private static AudioAnalysis Analysis(
        double lowDb = -18,
        double midDb = -16,
        double highDb = -20,
        double stereoWidth = 0.5) => new(
            IntegratedLufs: -14,
            TruePeakDb: -1,
            LoudnessRange: 7,
            CrestFactorDb: 10,
            LowEnergyDb: lowDb,
            MidEnergyDb: midDb,
            HighEnergyDb: highDb,
            StereoWidth: stereoWidth,
            DurationSeconds: 180,
            SampleRate: 44_100,
            Channels: 2);
}

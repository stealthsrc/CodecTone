using AudioConverter.Core.Remix;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class RemixPresetTests
{
    [TestMethod]
    public void BassBoost_CreatesBassAndHeadroom()
    {
        var rack = RemixPresetFactory.Create(RemixPreset.BassBoost);

        var bass = rack.OfType<BassEffect>().Single();
        var volume = rack.OfType<VolumeEffect>().Single();
        Assert.AreEqual(8, bass.GainDb);
        Assert.AreEqual(90, bass.FrequencyHz);
        Assert.AreEqual(-2, volume.GainDb);
    }

    [TestMethod]
    public void SlowedReverb_CreatesApprovedValues()
    {
        var rack = RemixPresetFactory.Create(RemixPreset.SlowedReverb);

        Assert.AreEqual(0.85, rack.OfType<TempoPitchEffect>().Single().Rate);
        Assert.AreEqual(0.28, rack.OfType<ReverbEffect>().Single().Mix);
        Assert.AreEqual(2.4, rack.OfType<ReverbEffect>().Single().DecaySeconds);
    }

    [TestMethod]
    public void SpedUpReverb_CreatesApprovedValues()
    {
        var rack = RemixPresetFactory.Create(RemixPreset.SpedUpReverb);

        Assert.AreEqual(1.18, rack.OfType<TempoPitchEffect>().Single().Rate);
        Assert.AreEqual(0.18, rack.OfType<ReverbEffect>().Single().Mix);
        Assert.AreEqual(1.6, rack.OfType<ReverbEffect>().Single().DecaySeconds);
    }
}

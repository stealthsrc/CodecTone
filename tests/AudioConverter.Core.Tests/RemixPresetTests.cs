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

    [TestMethod]
    public void Nightcore_UsesFastTempoAndBrightEq()
    {
        var rack = RemixPresetFactory.Create(RemixPreset.Nightcore);

        Assert.AreEqual(1.25, rack.OfType<TempoPitchEffect>().Single().Rate);
        Assert.AreEqual(3, rack.OfType<EqualizerEffect>().Single().HighGainDb);
    }

    [TestMethod]
    public void DeepBass_UsesControlledLowFrequencyBoost()
    {
        var rack = RemixPresetFactory.Create(RemixPreset.DeepBass);

        var bass = rack.OfType<BassEffect>().Single();
        Assert.AreEqual(12, bass.GainDb);
        Assert.AreEqual(70, bass.FrequencyHz);
        Assert.IsNotNull(rack.OfType<LoudnessNormalizeEffect>().SingleOrDefault());
    }

    [DataTestMethod]
    [DataRow(RemixPreset.VocalBoost)]
    [DataRow(RemixPreset.DreamyReverb)]
    [DataRow(RemixPreset.LoFi)]
    [DataRow(RemixPreset.Club)]
    [DataRow(RemixPreset.AcousticWarmth)]
    [DataRow(RemixPreset.Telephone)]
    public void ExpandedPreset_CreatesValidEditableRack(RemixPreset preset)
    {
        var rack = RemixPresetFactory.Create(preset);

        Assert.IsTrue(rack.Count > 0);
        RemixRackValidator.Validate(rack, 180);
    }
}

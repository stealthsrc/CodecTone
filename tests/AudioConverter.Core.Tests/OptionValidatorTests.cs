using AudioConverter.Core.Models;
using AudioConverter.Core.Validation;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class OptionValidatorTests
{
    [TestMethod]
    public void Validate_RejectsBitrateForLosslessOutput()
    {
        var options = new ConversionOptions(AudioFormat.Flac, Bitrate: "192k");
        Assert.ThrowsException<ArgumentException>(() => OptionValidator.Validate(options));
    }

    [TestMethod]
    public void Validate_RejectsSampleSettingsForLossyOutput()
    {
        var options = new ConversionOptions(AudioFormat.Mp3, SampleRate: 48_000);
        Assert.ThrowsException<ArgumentException>(() => OptionValidator.Validate(options));
    }

    [TestMethod]
    public void Validate_RejectsThirtyTwoBitFlac()
    {
        var options = new ConversionOptions(AudioFormat.Flac, BitDepth: 32);
        Assert.ThrowsException<ArgumentException>(() => OptionValidator.Validate(options));
    }

    [TestMethod]
    public void Validate_AcceptsSupportedMp3Options()
    {
        var options = new ConversionOptions(AudioFormat.Mp3, Bitrate: "320k");
        OptionValidator.Validate(options);
    }
}

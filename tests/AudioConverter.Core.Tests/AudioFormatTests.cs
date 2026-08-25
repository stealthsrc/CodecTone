using AudioConverter.Core.Models;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class AudioFormatTests
{
    [TestMethod]
    public void FromPath_IsCaseInsensitive()
    {
        Assert.AreEqual(AudioFormat.M4a, AudioFormats.FromPath("Track.M4A"));
    }

    [TestMethod]
    public void FromPath_RejectsUnsupportedExtension()
    {
        Assert.ThrowsException<ArgumentException>(() => AudioFormats.FromPath("clip.mp4"));
    }
}

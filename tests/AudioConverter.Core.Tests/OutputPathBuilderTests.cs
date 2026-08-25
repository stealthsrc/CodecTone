using AudioConverter.Core.Models;
using AudioConverter.Core.Paths;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class OutputPathBuilderTests
{
    [TestMethod]
    public void Build_UsesSuffixAndDestinationFormat()
    {
        var path = OutputPathBuilder.Build(
            "music/My Track.flac",
            "exports",
            AudioFormat.Ogg,
            "_mobile");

        Assert.AreEqual(Path.Combine("exports", "My Track_mobile.ogg"), path);
    }

    [TestMethod]
    public void Build_RejectsInvalidSuffixCharacters()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            OutputPathBuilder.Build("track.flac", "exports", AudioFormat.Mp3, "bad/name"));
    }
}

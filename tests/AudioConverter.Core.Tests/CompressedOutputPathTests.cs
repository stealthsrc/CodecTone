using AudioConverter.Core.Models;
using AudioConverter.Core.Paths;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class CompressedOutputPathTests
{
    [TestMethod]
    public void BuildCompressed_PreservesRelativeSubdirectories()
    {
        var sourceRoot = Path.Combine("C:", "Music");
        var source = Path.Combine(sourceRoot, "Album", "Disc 1", "track.flac");
        var outputRoot = Path.Combine("D:", "Compressed");

        var output = OutputPathBuilder.BuildCompressed(
            source, sourceRoot, outputRoot, AudioFormat.Mp3, "_small");

        Assert.AreEqual(
            Path.Combine(outputRoot, "Album", "Disc 1", "track_small.mp3"),
            output);
    }
}

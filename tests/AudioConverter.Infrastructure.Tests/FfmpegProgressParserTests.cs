using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfmpegProgressParserTests
{
    [TestMethod]
    public void Parse_ClampsRunningProgressBelowCompletion()
    {
        Assert.AreEqual(0.5, FfmpegProgressParser.Parse("out_time_us=5000000", 10)!.Value, 0.001);
        Assert.AreEqual(0.99, FfmpegProgressParser.Parse("out_time_ms=20000000", 10)!.Value, 0.001);
    }

    [TestMethod]
    public void Parse_ReturnsCompletionMarker()
    {
        Assert.AreEqual(1.0, FfmpegProgressParser.Parse("progress=end", 10));
        Assert.IsNull(FfmpegProgressParser.Parse("speed=1.2x", 10));
    }
}

using AudioConverter.Infrastructure.Remix;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class LoudnessParserTests
{
    [TestMethod]
    public void Parse_ReadsFfmpegLoudnormJson()
    {
        const string output = """
        [Parsed_loudnorm_0] {
          "input_i" : "-20.10",
          "input_tp" : "-1.20",
          "input_lra" : "4.30",
          "input_thresh" : "-30.00",
          "target_offset" : "1.50"
        }
        """;

        var result = LoudnessParser.Parse(output);

        Assert.AreEqual(-20.1, result.InputIntegrated);
        Assert.AreEqual(-1.2, result.InputTruePeak);
        Assert.AreEqual(4.3, result.InputLoudnessRange);
        Assert.AreEqual(-30, result.InputThreshold);
        Assert.AreEqual(1.5, result.TargetOffset);
    }
}

using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfprobeParserTests
{
    [TestMethod]
    public void Parse_ValidatesAudioAndReadsDurationAndTags()
    {
        const string json = """
        {"streams":[{"codec_type":"audio","codec_name":"flac","sample_rate":"48000","bits_per_raw_sample":"24","bit_rate":"921600"},{"codec_type":"video","disposition":{"attached_pic":1}}],"format":{"duration":"123.456","size":"15123456","bit_rate":"980000","tags":{"title":"Track","artist":"Artist","album":"Album"}}}
        """;

        var result = FfprobeParser.Parse(json);

        Assert.AreEqual(123.456, result.DurationSeconds, 0.001);
        Assert.AreEqual("flac", result.CodecName);
        Assert.AreEqual(48000, result.SampleRate);
        Assert.AreEqual(24, result.BitDepth);
        Assert.IsTrue(result.HasCoverArt);
        Assert.AreEqual("Track", result.Tags["title"]);
        Assert.AreEqual(15_123_456, result.SizeBytes);
        Assert.AreEqual(921, result.AudioBitrateKbps);
        Assert.AreEqual(980, result.ContainerBitrateKbps);
    }

    [TestMethod]
    public void Parse_RejectsInputWithoutAudioStream()
    {
        Assert.ThrowsException<InvalidDataException>(() =>
            FfprobeParser.Parse("{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{}}"));
    }
}

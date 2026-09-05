using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfprobeParserTests
{
    [TestMethod]
    public void Parse_ValidatesAudioAndReadsDurationAndTags()
    {
        const string json = """
        {"streams":[{"codec_type":"audio","codec_name":"flac","sample_rate":"48000","channels":2,"bits_per_raw_sample":"24","bit_rate":"921600"},{"codec_type":"video","disposition":{"attached_pic":1}}],"format":{"duration":"123.456","size":"15123456","bit_rate":"980000","tags":{"title":"Track","artist":"Artist","album":"Album"}}}
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
        Assert.AreEqual(2, result.Channels);
    }

    [TestMethod]
    public void Parse_RejectsInputWithoutAudioStream()
    {
        Assert.ThrowsException<InvalidDataException>(() =>
            FfprobeParser.Parse("{\"streams\":[{\"codec_type\":\"video\"}],\"format\":{}}"));
    }

    [TestMethod]
    public void Parse_ReportsEmbeddedArtworkStreamsAndFrontCoverTag()
    {
        const string json = """
        {
          "streams": [
            {"index":0,"codec_type":"audio","codec_name":"flac"},
            {"index":1,"codec_type":"video","codec_name":"png","width":600,"height":600,"disposition":{"attached_pic":1},"tags":{"comment":"Cover (back)"}},
            {"index":2,"codec_type":"video","codec_name":"mjpeg","width":1200,"height":1200,"disposition":{"attached_pic":1},"tags":{"comment":"Cover (front)"}}
          ],
          "format":{"duration":"10","tags":{"artist":"Artist","album":"Album"}}
        }
        """;

        var result = FfprobeParser.Parse(json);

        Assert.AreEqual(2, result.ArtworkStreams.Count);
        Assert.IsTrue(result.ArtworkStreams.Single(stream => stream.StreamIndex == 2).IsFrontCover);
        Assert.AreEqual(1200, result.ArtworkStreams.Single(stream => stream.StreamIndex == 2).Width);
    }

    [TestMethod]
    public void Parse_DoesNotTreatRegularVideoAsAlbumArtwork()
    {
        const string json = """
        {"streams":[{"index":0,"codec_type":"audio","codec_name":"aac"},{"index":1,"codec_type":"video","codec_name":"h264","width":1920,"height":1080,"disposition":{"attached_pic":0}}],"format":{"duration":"10"}}
        """;

        Assert.AreEqual(0, FfprobeParser.Parse(json).ArtworkStreams.Count);
    }
}

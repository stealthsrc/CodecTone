using AudioConverter.Core.Remix;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class RemixMetadataTests
{
    [TestMethod]
    public void Validate_AcceptsStandardAndCustomTags()
    {
        var metadata = new RemixMetadata(
            Title: "Track",
            Artist: "Artist",
            CustomTags: new Dictionary<string, string> { ["remix.by"] = "CodecTone" });

        RemixMetadataValidator.Validate(metadata);
    }

    [TestMethod]
    public void Validate_RejectsUnsafeCustomTagKey()
    {
        var metadata = new RemixMetadata(
            CustomTags: new Dictionary<string, string> { ["bad key="] = "value" });

        Assert.ThrowsException<ArgumentException>(() => RemixMetadataValidator.Validate(metadata));
    }
}

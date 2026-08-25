using AudioConverter.Infrastructure.Remix;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class RemixImpulseResponseProviderTests
{
    [TestMethod]
    public void EnsureExtracted_WritesEmbeddedWaveAsset()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var path = RemixImpulseResponseProvider.EnsureExtracted(root);

            Assert.IsTrue(File.Exists(path));
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[4];
            Assert.AreEqual(4, stream.Read(header));
            CollectionAssert.AreEqual("RIFF"u8.ToArray(), header.ToArray());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}

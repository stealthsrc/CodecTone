using AudioConverter.Core.Compression;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class CompressionFileDiscoveryTests
{
    [TestMethod]
    public void Find_RecursivelyReturnsSupportedFilesAndRelativePaths()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Album"));
        File.WriteAllBytes(Path.Combine(root, "root.mp3"), [1]);
        File.WriteAllBytes(Path.Combine(root, "Album", "track.flac"), [1]);
        File.WriteAllBytes(Path.Combine(root, "Album", "cover.jpg"), [1]);
        try
        {
            var files = CompressionFileDiscovery.Find(root, recursive: true);

            Assert.AreEqual(2, files.Count);
            CollectionAssert.Contains(files.Select(file => file.RelativePath).ToList(), "root.mp3");
            CollectionAssert.Contains(files.Select(file => file.RelativePath).ToList(), Path.Combine("Album", "track.flac"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Find_NonRecursiveExcludesNestedFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Nested"));
        File.WriteAllBytes(Path.Combine(root, "root.wav"), [1]);
        File.WriteAllBytes(Path.Combine(root, "Nested", "track.wav"), [1]);
        try
        {
            var files = CompressionFileDiscovery.Find(root, recursive: false);

            Assert.AreEqual(1, files.Count);
            Assert.AreEqual("root.wav", files[0].RelativePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

}

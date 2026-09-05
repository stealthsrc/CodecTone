using AudioConverter.Infrastructure.Artwork;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class ArtworkCollisionResolverTests
{
    [TestMethod]
    public async Task ResolveAsync_SkipsExistingIdenticalImage()
    {
        using var directory = new TemporaryDirectory();
        var staged = directory.File("staged.png", [1, 2, 3]);
        var desired = directory.File("Artist - Album.png", [1, 2, 3]);

        var result = await ArtworkCollisionResolver.ResolveAsync(staged, desired);

        Assert.IsTrue(result.IsIdentical);
        Assert.AreEqual(desired, result.OutputPath);
    }

    [TestMethod]
    public async Task ResolveAsync_NumbersDifferentCollisionAndReusesMatchingNumber()
    {
        using var directory = new TemporaryDirectory();
        var staged = directory.File("staged.jpg", [9, 8, 7]);
        _ = directory.File("Artist - Album.jpg", [1]);
        var second = directory.File("Artist - Album_2.jpg", [9, 8, 7]);

        var result = await ArtworkCollisionResolver.ResolveAsync(staged, Path.Combine(directory.Path, "Artist - Album.jpg"));

        Assert.IsTrue(result.IsIdentical);
        Assert.AreEqual(second, result.OutputPath);
    }

    [TestMethod]
    public async Task ResolveAsync_ReturnsNextAvailableNumber()
    {
        using var directory = new TemporaryDirectory();
        var staged = directory.File("staged.webp", [3]);
        _ = directory.File("Artist - Album.webp", [1]);
        _ = directory.File("Artist - Album_2.webp", [2]);

        var result = await ArtworkCollisionResolver.ResolveAsync(staged, Path.Combine(directory.Path, "Artist - Album.webp"));

        Assert.IsFalse(result.IsIdentical);
        StringAssert.EndsWith(result.OutputPath, "Artist - Album_3.webp");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"codectone-artwork-{Guid.NewGuid():N}");
        public TemporaryDirectory() => Directory.CreateDirectory(Path);
        public string File(string name, byte[] bytes)
        {
            var path = System.IO.Path.Combine(Path, name);
            System.IO.File.WriteAllBytes(path, bytes);
            return path;
        }
        public void Dispose() => Directory.Delete(Path, true);
    }
}

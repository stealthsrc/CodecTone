using AudioConverter.Core.Artwork;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class ArtworkPlannerTests
{
    [TestMethod]
    public void Create_DeduplicatesTracksFromSameAlbum()
    {
        var artwork = new EmbeddedArtworkInfo(1, "mjpeg", 1200, 1200, true, true);
        ArtworkSource[] sources =
        [
            Source("01.flac", "Artist", "Album", artwork),
            Source("02.flac", "artist", "album", artwork with { StreamIndex = 2 }),
        ];

        var plan = ArtworkPlanner.Create(sources);

        Assert.AreEqual(1, plan.Albums.Count);
        Assert.AreEqual("Artist - Album", plan.Albums[0].OutputBaseName);
        Assert.AreEqual("01.flac", Path.GetFileName(plan.Albums[0].SourcePath));
        Assert.AreEqual(1, plan.Skipped.Count);
    }

    [TestMethod]
    public void Create_PrefersFrontCoverOverFirstAttachedImage()
    {
        var first = new EmbeddedArtworkInfo(1, "png", 500, 500, true, false);
        var front = new EmbeddedArtworkInfo(2, "mjpeg", 1000, 1000, true, true);

        var album = ArtworkPlanner.Create([Source("song.m4a", "Artist", "Album", first, front)]).Albums.Single();

        Assert.AreEqual(2, album.Artwork.StreamIndex);
    }

    [TestMethod]
    public void Create_UsesDirectoryFallbacksAndSanitizesWindowsName()
    {
        var root = Path.Combine("C:\\Music", "Artist Name", "Album: Deluxe");
        var source = new ArtworkSource(
            Path.Combine(root, "01 - Track.flac"),
            new Dictionary<string, string>(),
            [new EmbeddedArtworkInfo(1, "png", 800, 800, true, false)]);

        var album = ArtworkPlanner.Create([source]).Albums.Single();

        Assert.AreEqual("Artist Name - Album_ Deluxe", album.OutputBaseName);
    }

    [TestMethod]
    public void Create_ReportsSourceWithoutArtworkAsSkipped()
    {
        var plan = ArtworkPlanner.Create([new ArtworkSource("song.mp3", new Dictionary<string, string>(), [])]);

        Assert.AreEqual(0, plan.Albums.Count);
        StringAssert.Contains(plan.Skipped.Single().Reason, "no embedded artwork");
    }

    [TestMethod]
    public void Create_BoundsOutputNameFromOversizedMetadata()
    {
        var longValue = new string('A', 300);
        var album = ArtworkPlanner.Create([
            Source("song.flac", longValue, longValue, new EmbeddedArtworkInfo(1, "png", 800, 800, true, true)),
        ]).Albums.Single();

        Assert.IsTrue(album.OutputBaseName.Length <= 180);
    }

    private static ArtworkSource Source(string name, string artist, string album, params EmbeddedArtworkInfo[] artwork) =>
        new(
            Path.Combine("C:\\Music", album, name),
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["artist"] = artist, ["album"] = album },
            artwork);
}

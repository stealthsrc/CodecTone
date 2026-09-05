using AudioConverter.Core.Artwork;
namespace AudioConverter.Core.Tests;
[TestClass]
public sealed class ArtworkSelectionRegressionTests
{
    [TestMethod]
    public void Album_SelectsLargestFrontAcrossTracksAndRetainsFallbacks()
    {
        var tags = new Dictionary<string,string> { ["artist"]="Artist", ["album"]="Album" };
        var plan = ArtworkPlanner.Create([
            new ArtworkSource("first.flac", tags, [new EmbeddedArtworkInfo(1,"png",2000,2000,true,false)]),
            new ArtworkSource("second.flac", tags, [new EmbeddedArtworkInfo(1,"png",500,500,true,true)]),
            new ArtworkSource("third.flac", tags, [new EmbeddedArtworkInfo(1,"png",1000,1000,true,true)])]);
        Assert.AreEqual("third.flac", plan.Albums.Single().SourcePath);
        Assert.AreEqual(2, plan.Skipped.Count);
    }
}

using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfmpegLocatorTests
{
    [TestMethod]
    public void Find_UsesCompleteManagedPair()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var bin = Path.Combine(root, "runtime", "ffmpeg", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllBytes(Path.Combine(bin, "ffmpeg.exe"), [0x4d, 0x5a]);
        File.WriteAllBytes(Path.Combine(bin, "ffprobe.exe"), [0x4d, 0x5a]);
        try
        {
            var tools = FfmpegLocator.Find(root, _ => null);
            Assert.AreEqual(Path.Combine(bin, "ffmpeg.exe"), tools.FfmpegPath);
            Assert.AreEqual(Path.Combine(bin, "ffprobe.exe"), tools.FfprobePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Find_RejectsIncompleteInstall()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "runtime", "ffmpeg", "bin"));
        try
        {
            Assert.ThrowsException<FfmpegDependencyException>(() => FfmpegLocator.Find(root, _ => null));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public void Find_PrefersManagedPairOverPathExecutables()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var bin = Path.Combine(root, "runtime", "ffmpeg", "bin");
        Directory.CreateDirectory(bin);
        File.WriteAllBytes(Path.Combine(bin, "ffmpeg.exe"), [0x4d, 0x5a]);
        File.WriteAllBytes(Path.Combine(bin, "ffprobe.exe"), [0x4d, 0x5a]);
        try
        {
            var tools = FfmpegLocator.Find(root, name => Path.Combine("C:\\Untrusted", name));

            Assert.AreEqual(Path.Combine(bin, "ffmpeg.exe"), tools.FfmpegPath);
            Assert.AreEqual(Path.Combine(bin, "ffprobe.exe"), tools.FfprobePath);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}

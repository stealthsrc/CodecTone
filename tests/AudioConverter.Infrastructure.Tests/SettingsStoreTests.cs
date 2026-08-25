using AudioConverter.Core.Models;
using AudioConverter.Infrastructure.Storage;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var store = new JsonSettingsStore(root);
            await store.SaveAsync(new AppSettings("White", "C:\\Audio"));

            Assert.AreEqual(new AppSettings("White", "C:\\Audio"), await store.LoadAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [TestMethod]
    public async Task Load_ReturnsDefaultsForInvalidJson()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "not json");
        try
        {
            Assert.AreEqual(new AppSettings(), await new JsonSettingsStore(root).LoadAsync());
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}

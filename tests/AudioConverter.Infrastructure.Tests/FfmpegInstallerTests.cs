using System.Security.Cryptography;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfmpegInstallerTests
{
    [TestMethod]
    public async Task VerifySha256_AcceptsExpectedDigest()
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, "ffmpeg");
        try
        {
            var expected = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path)));
            await FfmpegInstaller.VerifySha256Async(path, expected);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task VerifySha256_RejectsMismatch()
    {
        var path = Path.GetTempFileName();
        try
        {
            await Assert.ThrowsExceptionAsync<FfmpegInstallException>(() =>
                FfmpegInstaller.VerifySha256Async(path, new string('0', 64)));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

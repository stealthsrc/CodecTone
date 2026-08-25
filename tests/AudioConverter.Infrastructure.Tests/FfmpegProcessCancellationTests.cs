using System.Diagnostics;
using AudioConverter.Infrastructure.Ffmpeg;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class FfmpegProcessCancellationTests
{
    [TestMethod]
    public async Task WaitForExitOrKillAsync_KillsProcessTreeWhenCancelled()
    {
        using var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c ping -n 30 127.0.0.1 > nul")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
            FfmpegProcessRunner.WaitForExitOrKillAsync(process, cancellation.Token));

        Assert.IsTrue(process.HasExited);
    }
}

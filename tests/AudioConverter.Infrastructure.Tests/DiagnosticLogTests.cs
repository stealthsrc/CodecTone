using AudioConverter.Infrastructure.Storage;
namespace AudioConverter.Infrastructure.Tests;
[TestClass]
public sealed class DiagnosticLogTests
{
    [TestMethod]
    public void Log_RotatesAndStaysBounded()
    {
        var root = Path.Combine(Path.GetTempPath(), $"log-{Guid.NewGuid():N}");
        try
        {
            var log = new LocalDiagnosticLog(root);
            for (var i = 0; i < 400; i++) log.Write("Test", new string('x', 4000));
            Assert.IsTrue(File.Exists(log.FilePath + ".previous"));
            Assert.IsTrue(Directory.GetFiles(root).Sum(f => new FileInfo(f).Length) < 1100000);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}

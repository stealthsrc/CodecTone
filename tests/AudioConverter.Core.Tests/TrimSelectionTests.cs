using AudioConverter.Core.Models;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class TrimSelectionTests
{
    [TestMethod]
    public void Create_RejectsFadesLongerThanSelection()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            TrimSelection.Create(5, 10, fadeInSeconds: 3, fadeOutSeconds: 3));
    }

    [TestMethod]
    public void Duration_IsEndMinusStart()
    {
        var selection = TrimSelection.Create(14.06, 164.48, 2, 3);
        Assert.AreEqual(150.42, selection.DurationSeconds, 0.0001);
    }
}

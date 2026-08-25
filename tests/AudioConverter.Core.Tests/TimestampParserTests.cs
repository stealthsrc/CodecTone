using AudioConverter.Core.Validation;

namespace AudioConverter.Core.Tests;

[TestClass]
public sealed class TimestampParserTests
{
    [DataTestMethod]
    [DataRow("12.5", 12.5)]
    [DataRow("01:02.500", 62.5)]
    [DataRow("01:02:03.250", 3723.25)]
    public void Parse_AcceptsSupportedForms(string value, double expected)
    {
        Assert.AreEqual(expected, TimestampParser.Parse(value), 0.0001);
    }

    [TestMethod]
    public void Parse_RejectsInvalidComponents()
    {
        Assert.ThrowsException<FormatException>(() => TimestampParser.Parse("01:60"));
        Assert.ThrowsException<FormatException>(() => TimestampParser.Parse("-1"));
    }

    [TestMethod]
    public void Format_UsesMilliseconds()
    {
        Assert.AreEqual("00:01:02.500", TimestampParser.Format(62.5));
    }
}

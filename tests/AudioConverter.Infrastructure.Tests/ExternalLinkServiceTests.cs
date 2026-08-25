using AudioConverter.Infrastructure.Shell;

namespace AudioConverter.Infrastructure.Tests;

[TestClass]
public sealed class ExternalLinkServiceTests
{
    [TestMethod]
    public void GitHubProfile_UsesTheApprovedHttpsProfile()
    {
        Assert.AreEqual("https://github.com/stealthsrc", ExternalLinkService.GitHubProfile.AbsoluteUri.TrimEnd('/'));
        Assert.IsTrue(ExternalLinkService.IsAllowedGitHubProfile(ExternalLinkService.GitHubProfile));
    }

    [DataTestMethod]
    [DataRow("http://github.com/stealthsrc")]
    [DataRow("https://github.com/other")]
    [DataRow("https://github.com.evil.test/stealthsrc")]
    public void IsAllowedGitHubProfile_RejectsUnapprovedUris(string value)
    {
        Assert.IsFalse(ExternalLinkService.IsAllowedGitHubProfile(new Uri(value)));
    }
}

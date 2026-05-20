using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowsingHistoryPolicyTests
{
    [Theory]
    [InlineData("https://example.com", true)]
    [InlineData("http://localhost:8080", true)]
    [InlineData("file:///C:/temp/page.html", true)]
    public void ShouldRecord_ValidUrls_ReturnsTrue(string url, bool expected) =>
        Assert.Equal(expected, BrowsingHistoryPolicy.ShouldRecord(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("about:blank")]
    [InlineData("javascript:void(0)")]
    [InlineData("data:text/html,hello")]
    public void ShouldRecord_InvalidUrls_ReturnsFalse(string? url) =>
        Assert.False(BrowsingHistoryPolicy.ShouldRecord(url));
}
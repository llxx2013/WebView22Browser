using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptUrlMatcherTests
{
    [Theory]
    [InlineData("https://www.example.com/foo", "*://*.example.com/*", true)]
    [InlineData("https://foo.example.com/bar", "*://*.example.com/*", true)]
    [InlineData("https://other.com/", "*://*.example.com/*", false)]
    [InlineData("https://example.com/path", "https://example.com/*", true)]
    [InlineData("http://example.com/", "https://example.com/*", false)]
    public void IsMatch_WildcardPatterns(string url, string pattern, bool expected) =>
        Assert.Equal(expected, UserScriptUrlMatcher.IsMatch(url, pattern));

    [Fact]
    public void IsMatch_FileUrl_RejectedByDefault() =>
        Assert.False(UserScriptUrlMatcher.IsMatch("file:///C:/test.html", "*://*/*"));

    [Fact]
    public void IsMatch_FileUrl_AllowedWhenPatternExplicit() =>
        Assert.True(UserScriptUrlMatcher.IsMatch("file:///C:/test.html", "file://*/*"));

    [Fact]
    public void IsMatch_AboutBlank_Rejected() =>
        Assert.False(UserScriptUrlMatcher.IsMatch("about:blank", "*://*/*"));

    [Fact]
    public void IsMatchAny_ReturnsTrueWhenOnePatternMatches()
    {
        var patterns = new[] { "https://a.com/*", "*://*.bing.com/*" };
        Assert.True(UserScriptUrlMatcher.IsMatchAny("https://www.bing.com/", patterns));
    }
}
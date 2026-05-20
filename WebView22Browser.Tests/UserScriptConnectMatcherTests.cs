using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptConnectMatcherTests
{
    [Fact]
    public void IsAllowed_WildcardConnect_AllowsAnyHttpHost()
    {
        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://api.example.org/data",
            ["*"],
            []));
    }

    [Fact]
    public void IsAllowed_Self_MatchesMatchPatternHost()
    {
        var matchPatterns = new[] { "https://www.example.com/*" };

        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://www.example.com/api",
            ["self"],
            matchPatterns));

        Assert.False(UserScriptConnectMatcher.IsAllowed(
            "https://other.example.com/api",
            ["self"],
            matchPatterns));
    }

    [Fact]
    public void IsAllowed_SubdomainPattern_AllowsSubdomains()
    {
        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://api.example.com/x",
            ["*.example.com"],
            []));

        Assert.False(UserScriptConnectMatcher.IsAllowed(
            "https://example.com/x",
            ["*.example.com"],
            []));
    }

    [Fact]
    public void IsAllowed_DomainPattern_AllowsRootAndSubdomains()
    {
        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://example.com/",
            ["example.com"],
            []));

        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://api.example.com/",
            ["example.com"],
            []));
    }

    [Theory]
    [InlineData("ftp://example.com/")]
    [InlineData("file:///C:/temp")]
    [InlineData("not-a-url")]
    public void IsAllowed_NonHttpScheme_ReturnsFalse(string url)
    {
        Assert.False(UserScriptConnectMatcher.IsAllowed(url, ["*"], []));
    }

    [Fact]
    public void IsAllowed_EmptyConnectPatterns_DefaultsToSelf()
    {
        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://bing.com/search",
            [],
            ["https://bing.com/*"]));
    }

    [Fact]
    public void IsAllowed_SelfWithWildcardMatch_DoesNotImplyConnectAll()
    {
        // `@match *://*/*` + `@connect self` historically degenerated into `@connect *`
        // because the extracted host was the bare `*` wildcard. Self must still require a
        // concrete host segment.
        Assert.False(UserScriptConnectMatcher.IsAllowed(
            "https://unrelated.org/data",
            ["self"],
            ["*://*/*"]));
    }

    [Fact]
    public void IsAllowed_SelfWithMixedPatterns_AcceptsConcreteOnly()
    {
        // A wildcard host alongside a concrete host should still allow the concrete host
        // (and only that one).
        Assert.True(UserScriptConnectMatcher.IsAllowed(
            "https://api.example.com/v1",
            ["self"],
            ["*://*/*", "https://api.example.com/*"]));

        Assert.False(UserScriptConnectMatcher.IsAllowed(
            "https://other.net/v1",
            ["self"],
            ["*://*/*", "https://api.example.com/*"]));
    }
}

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class SecurityStateDevToolsParserTests
{
    [Theory]
    [InlineData("""{"visibleSecurityState":{"securityState":"secure"}}""", AddressBarSecurityState.Secure)]
    [InlineData("""{"visibleSecurityState":{"securityState":"neutral"}}""", AddressBarSecurityState.Neutral)]
    [InlineData("""{"visibleSecurityState":{"securityState":"info"}}""", AddressBarSecurityState.Neutral)]
    [InlineData("""{"visibleSecurityState":{"securityState":"insecure"}}""", AddressBarSecurityState.Insecure)]
    [InlineData("""{"visibleSecurityState":{"securityState":"insecure-broken"}}""", AddressBarSecurityState.Dangerous)]
    [InlineData("""{"visibleSecurityState":{"securityState":"dangerous"}}""", AddressBarSecurityState.Dangerous)]
    [InlineData("""{"visibleSecurityState":{"securityState":"unknown"}}""", AddressBarSecurityState.Unknown)]
    [InlineData("{}", AddressBarSecurityState.Unknown)]
    [InlineData(null, AddressBarSecurityState.Unknown)]
    public void ParseVisibleSecurityStateChanged_MapsStates(string? json, AddressBarSecurityState expected)
    {
        var result = SecurityStateDevToolsParser.ParseVisibleSecurityStateChanged(json);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://example.com/", AddressBarSecurityState.Secure)]
    [InlineData("http://example.com/", AddressBarSecurityState.Insecure)]
    [InlineData("about:blank", AddressBarSecurityState.Unknown)]
    [InlineData("file:///C:/test.html", AddressBarSecurityState.Unknown)]
    [InlineData("", AddressBarSecurityState.Unknown)]
    [InlineData(null, AddressBarSecurityState.Unknown)]
    public void FromUriScheme_MapsSchemes(string? uri, AddressBarSecurityState expected)
    {
        var result = SecurityStateDevToolsParser.FromUriScheme(uri);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("https://example.com/", true)]
    [InlineData("http://example.com/", true)]
    [InlineData("about:blank", false)]
    [InlineData("file:///C:/x", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void UsesHttpSecurityScheme_DetectsHttpSchemes(string? uri, bool expected)
    {
        var result = SecurityStateDevToolsParser.UsesHttpSecurityScheme(uri);
        Assert.Equal(expected, result);
    }
}

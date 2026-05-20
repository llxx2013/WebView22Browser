using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

/// <summary>
/// Ensures production C# matcher agrees with bootstrap JavaScript semantics (via JS parity port).
/// </summary>
public class UserScriptUrlMatcherParityTests
{
    [Theory]
    [MemberData(nameof(UserScriptMatchTestVectors.AllCases), MemberType = typeof(UserScriptMatchTestVectors))]
    public void CSharpMatcher_MatchesExpected(string url, string pattern, bool expected) =>
        Assert.Equal(expected, UserScriptUrlMatcher.IsMatch(url, pattern));

    [Theory]
    [MemberData(nameof(UserScriptMatchTestVectors.AllCases), MemberType = typeof(UserScriptMatchTestVectors))]
    public void JsParityMatcher_MatchesExpected(string url, string pattern, bool expected) =>
        Assert.Equal(expected, UserScriptUrlMatcherJsParity.IsMatch(url, pattern));

    [Theory]
    [MemberData(nameof(UserScriptMatchTestVectors.AllCases), MemberType = typeof(UserScriptMatchTestVectors))]
    public void CSharpAndJsParity_AgreeOnAllVectors(string url, string pattern, bool expected)
    {
        var csharp = UserScriptUrlMatcher.IsMatch(url, pattern);
        var jsParity = UserScriptUrlMatcherJsParity.IsMatch(url, pattern);

        Assert.Equal(expected, csharp);
        Assert.Equal(expected, jsParity);
        Assert.Equal(csharp, jsParity);
    }
}
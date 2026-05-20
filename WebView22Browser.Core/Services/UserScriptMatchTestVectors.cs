namespace WebView22Browser.Core.Services;

/// <summary>
/// Shared URL/pattern cases for C# matcher tests and JS parity cross-validation.
/// Keep in sync with matchUrl logic in <see cref="UserScriptBootstrapBuilder"/>.
/// </summary>
public static class UserScriptMatchTestVectors
{
    public static IEnumerable<object[]> AllCases()
    {
        foreach (var c in Cases)
            yield return [c.Url, c.Pattern, c.Expected];
    }

    public static IReadOnlyList<MatchCase> Cases { get; } =
    [
        new("https://www.example.com/foo", "*://*.example.com/*", true),
        new("https://foo.example.com/bar", "*://*.example.com/*", true),
        new("https://other.com/", "*://*.example.com/*", false),
        new("https://example.com/path", "https://example.com/*", true),
        new("http://example.com/", "https://example.com/*", false),
        new("https://www.bing.com/search", "*://*.bing.com/*", true),
        new("https://www.bing.com/search", "https://www.bing.com/*", true),
        new("https://sub.bing.com/", "*://*.bing.com/*", true),
        new("file:///C:/test.html", "*://*/*", false),
        new("file:///C:/test.html", "file://*/*", true),
        new("about:blank", "*://*/*", false),
        new("https://www.example.com/admin/page", "*://*.example.com/*", true),
        new("https://example.com/admin/page", "https://example.com/admin/*", true),
        new("edge://settings/", "*://*/*", false),
        new("https://localhost:8080/test", "http://localhost/*", false),
        new("http://localhost:8080/test", "http://localhost/*", true),
    ];

    public sealed record MatchCase(string Url, string Pattern, bool Expected);
}
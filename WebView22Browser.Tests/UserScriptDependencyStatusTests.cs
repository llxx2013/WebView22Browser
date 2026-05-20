using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptDependencyStatusTests
{
    [Fact]
    public void FormatSummary_NoDependencies_ReturnsEmpty()
    {
        var entry = new UserScriptEntry();
        Assert.Equal(string.Empty, UserScriptDependencyStatus.FormatSummary(entry, null, isEnabled: true));
    }

    [Fact]
    public void FormatSummary_Disabled_IncludesDisabledLabel()
    {
        var entry = new UserScriptEntry
        {
            RequireUrls = ["https://example.com/a.js"],
            Resources = new Dictionary<string, string> { ["x"] = "https://example.com/x.css" }
        };

        Assert.Equal(
            "Requires 1 · Resources 1 · 已禁用",
            UserScriptDependencyStatus.FormatSummary(entry, null, isEnabled: false));
    }
}
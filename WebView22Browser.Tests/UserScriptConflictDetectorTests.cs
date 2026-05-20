using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptConflictDetectorTests
{
    [Fact]
    public void FindConflicts_OverlappingPatterns_ReturnsConflict()
    {
        var userPatterns = new[] { "*://*.bing.com/*" };
        var extensions = new[]
        {
            new ExtensionContentScriptInfo
            {
                ExtensionName = "AdBlock",
                ExtensionId = "ext1",
                MatchPatterns = ["https://www.bing.com/*"]
            }
        };

        var conflicts = UserScriptConflictDetector.FindConflicts(userPatterns, extensions);

        Assert.NotEmpty(conflicts);
        Assert.Equal("AdBlock", conflicts[0].ExtensionName);
    }

    [Fact]
    public void FindConflicts_NoOverlap_ReturnsEmpty()
    {
        var userPatterns = new[] { "*://*.example.com/*" };
        var extensions = new[]
        {
            new ExtensionContentScriptInfo
            {
                ExtensionName = "Other",
                ExtensionId = "ext1",
                MatchPatterns = ["*://*.other.org/*"]
            }
        };

        var conflicts = UserScriptConflictDetector.FindConflicts(userPatterns, extensions);

        Assert.Empty(conflicts);
    }
}

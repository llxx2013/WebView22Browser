using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowsingHistorySearchTests
{
    private static readonly BrowsingHistoryEntry[] Sample =
    [
        new() { Url = "https://github.com/foo", Title = "GitHub Repo" },
        new() { Url = "https://bing.com/search", Title = "Bing Search" }
    ];

    [Fact]
    public void Filter_EmptyQuery_ReturnsAll() =>
        Assert.Equal(2, BrowsingHistorySearch.Filter(Sample, null).Count());

    [Fact]
    public void Filter_MatchesTitle_CaseInsensitive()
    {
        var results = BrowsingHistorySearch.Filter(Sample, "github").ToList();
        Assert.Single(results);
        Assert.Contains("github", results[0].Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Filter_MatchesUrl()
    {
        var results = BrowsingHistorySearch.Filter(Sample, "bing.com").ToList();
        Assert.Single(results);
        Assert.Equal("Bing Search", results[0].Title);
    }
}
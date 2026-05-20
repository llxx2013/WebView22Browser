using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowsingHistoryTitleFormatterTests
{
    [Fact]
    public void Format_WithTitle_ReturnsTrimmedTitle() =>
        Assert.Equal("My Page", BrowsingHistoryTitleFormatter.Format("https://example.com", "  My Page  "));

    [Fact]
    public void Format_WithoutTitle_ReturnsHost() =>
        Assert.Equal("example.com", BrowsingHistoryTitleFormatter.Format("https://example.com/page", null));

    [Fact]
    public void Format_EmptyTitleAndInvalidUri_ReturnsUrl() =>
        Assert.Equal("not-a-uri", BrowsingHistoryTitleFormatter.Format("not-a-uri", ""));
}

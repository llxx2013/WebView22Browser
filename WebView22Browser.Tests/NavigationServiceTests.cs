using WebView22Browser.Core;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class NavigationServiceTests
{
    private readonly NavigationService _sut = new(new BrowserOptions
    {
        HomeUrl = "https://www.bing.com",
        SearchUrlTemplate = "https://www.bing.com/search?q={0}"
    });

    [Theory]
    [InlineData("", "https://www.bing.com")]
    [InlineData("   ", "https://www.bing.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("http://example.com/path", "http://example.com/path")]
    [InlineData("localhost:8080", "https://localhost:8080/")]
    [InlineData("127.0.0.1", "https://127.0.0.1/")]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("hello world", "https://www.bing.com/search?q=hello%20world")]
    [InlineData("not a url", "https://www.bing.com/search?q=not%20a%20url")]
    public void ResolveUri_ReturnsExpected(string input, string expected)
    {
        var result = _sut.ResolveUri(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveUri_InvalidAbsoluteHttps_FallsBackToSearch()
    {
        var result = _sut.ResolveUri("https://");
        Assert.StartsWith("https://www.bing.com/search?q=", result);
    }

    [Fact]
    public void ResolveUri_Null_ReturnsHomeUrl()
    {
        var result = _sut.ResolveUri(null);
        Assert.Equal("https://www.bing.com", result);
    }

    [Theory]
    [InlineData("file:///C:/Users/test.html")]
    [InlineData("FILE:///D:/docs/readme.txt")]
    public void ResolveUri_FileUrl_PreservesFileScheme(string input)
    {
        var result = _sut.ResolveUri(input);

        Assert.StartsWith("file:///", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveUri_FileUrl_DoesNotBecomeHttpsHost()
    {
        var result = _sut.ResolveUri("file:///C:/folder/page.html");

        Assert.NotEqual("https://file///", result);
        Assert.StartsWith("file:///", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveUri_VeryLongInput_DoesNotThrow()
    {
        var input = new string('a', 50_000);
        var result = _sut.ResolveUri(input);
        Assert.StartsWith("https://www.bing.com/search?q=", result);
    }
}
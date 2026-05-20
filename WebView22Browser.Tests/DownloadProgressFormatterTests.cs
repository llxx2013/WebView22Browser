using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class DownloadProgressFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    public void FormatBytes_ReturnsExpected(long bytes, string expected) =>
        Assert.Equal(expected, DownloadProgressFormatter.FormatBytes(bytes));

    [Fact]
    public void FormatPercent_WhenTotalUnknown_ReturnsEmpty() =>
        Assert.Equal(string.Empty, DownloadProgressFormatter.FormatPercent(100, 0));

    [Fact]
    public void FormatPercent_WhenTotalKnown_ReturnsPercent()
    {
        Assert.Equal("50%", DownloadProgressFormatter.FormatPercent(50, 100));
        Assert.Equal("100%", DownloadProgressFormatter.FormatPercent(150, 100));
    }

    [Fact]
    public void FormatProgressText_WithTotal_IncludesBothSizes() =>
        Assert.Equal("1 KB / 10 KB", DownloadProgressFormatter.FormatProgressText(1024, 10240));

    [Fact]
    public void GetProgressValue_WhenTotalUnknown_ReturnsNull() =>
        Assert.Null(DownloadProgressFormatter.GetProgressValue(10, 0));

    [Fact]
    public void GetProgressValue_WhenTotalKnown_ReturnsClampedPercent() =>
        Assert.Equal(25.0, DownloadProgressFormatter.GetProgressValue(25, 100));
}

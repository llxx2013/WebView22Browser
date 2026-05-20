using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class NavigationErrorFormatterTests
{
    [Theory]
    [InlineData(BrowserErrorStatus.Timeout, "连接超时")]
    [InlineData(BrowserErrorStatus.HostNameNotResolved, "无法解析主机名")]
    [InlineData(BrowserErrorStatus.CertificateExpired, "证书已过期")]
    public void Format_KnownStatus_ReturnsChineseMessage(BrowserErrorStatus status, string expected)
    {
        Assert.Equal(expected, NavigationErrorFormatter.Format(status));
    }

    [Fact]
    public void Format_UnknownStatus_IncludesEnumName()
    {
        var message = NavigationErrorFormatter.Format((BrowserErrorStatus)9999);
        Assert.Contains("9999", message);
    }
}

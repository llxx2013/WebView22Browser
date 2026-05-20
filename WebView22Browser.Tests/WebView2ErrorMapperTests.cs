using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.WebView2;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Tests;

public class WebView2ErrorMapperTests
{
    [Theory]
    [InlineData(CoreWebView2WebErrorStatus.Timeout, BrowserErrorStatus.Timeout)]
    [InlineData(CoreWebView2WebErrorStatus.HostNameNotResolved, BrowserErrorStatus.HostNameNotResolved)]
    [InlineData(CoreWebView2WebErrorStatus.OperationCanceled, BrowserErrorStatus.OperationCanceled)]
    public void ToBrowser_MapsKnownValues(CoreWebView2WebErrorStatus input, BrowserErrorStatus expected)
    {
        Assert.Equal(expected, WebView2ErrorMapper.ToBrowser(input));
    }

    [Fact]
    public void ToBrowser_UnknownValue_MapsToUnknown()
    {
        Assert.Equal(BrowserErrorStatus.Unknown, WebView2ErrorMapper.ToBrowser((CoreWebView2WebErrorStatus)99999));
    }
}
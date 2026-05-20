using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core.Models;

namespace WebView22Browser.App.WebView2;

public static class WebView2ErrorMapper
{
    public static BrowserErrorStatus ToBrowser(CoreWebView2WebErrorStatus status) =>
        status switch
        {
            CoreWebView2WebErrorStatus.Unknown => BrowserErrorStatus.Unknown,
            CoreWebView2WebErrorStatus.CertificateCommonNameIsIncorrect => BrowserErrorStatus.CertificateCommonNameIsIncorrect,
            CoreWebView2WebErrorStatus.CertificateExpired => BrowserErrorStatus.CertificateExpired,
            CoreWebView2WebErrorStatus.CertificateIsInvalid => BrowserErrorStatus.CertificateIsInvalid,
            CoreWebView2WebErrorStatus.CertificateRevoked => BrowserErrorStatus.CertificateRevoked,
            CoreWebView2WebErrorStatus.HostNameNotResolved => BrowserErrorStatus.HostNameNotResolved,
            CoreWebView2WebErrorStatus.Timeout => BrowserErrorStatus.Timeout,
            CoreWebView2WebErrorStatus.Disconnected => BrowserErrorStatus.Disconnected,
            CoreWebView2WebErrorStatus.ConnectionAborted => BrowserErrorStatus.ConnectionAborted,
            CoreWebView2WebErrorStatus.ConnectionReset => BrowserErrorStatus.ConnectionReset,
            CoreWebView2WebErrorStatus.RedirectFailed => BrowserErrorStatus.RedirectFailed,
            CoreWebView2WebErrorStatus.UnexpectedError => BrowserErrorStatus.UnexpectedError,
            CoreWebView2WebErrorStatus.OperationCanceled => BrowserErrorStatus.OperationCanceled,
            _ => BrowserErrorStatus.Unknown
        };
}
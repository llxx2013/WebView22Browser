using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class NavigationErrorFormatter
{
    public static string Format(BrowserErrorStatus errorStatus) =>
        errorStatus switch
        {
            BrowserErrorStatus.Unknown => "未知错误",
            BrowserErrorStatus.CertificateRevoked => "证书已吊销",
            BrowserErrorStatus.CertificateIsInvalid => "证书无效",
            BrowserErrorStatus.CertificateCommonNameIsIncorrect => "证书域名不匹配",
            BrowserErrorStatus.CertificateExpired => "证书已过期",
            BrowserErrorStatus.HostNameNotResolved => "无法解析主机名",
            BrowserErrorStatus.Timeout => "连接超时",
            BrowserErrorStatus.Disconnected => "连接已断开",
            BrowserErrorStatus.ConnectionAborted => "连接已中止",
            BrowserErrorStatus.ConnectionReset => "连接已重置",
            BrowserErrorStatus.RedirectFailed => "重定向失败",
            BrowserErrorStatus.UnexpectedError => "发生意外错误",
            _ => $"导航失败 ({errorStatus})"
        };
}

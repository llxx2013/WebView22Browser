namespace WebView22Browser.Core.Models;

public enum BrowserErrorStatus
{
    Unknown,
    CertificateCommonNameIsIncorrect,
    CertificateExpired,
    CertificateIsInvalid,
    CertificateRevoked,
    HostNameNotResolved,
    Timeout,
    Disconnected,
    ConnectionAborted,
    ConnectionReset,
    RedirectFailed,
    UnexpectedError,
    OperationCanceled
}
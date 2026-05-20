using Microsoft.Web.WebView2.Core;

namespace WebView22Browser.App.Services;

public interface IDialogService
{
    void ShowError(string message, string title);

    void ShowWarning(string message, string title);

    void ShowInfo(string message, string title);

    bool Confirm(string message, string title);

    string? PromptSaveFile(string suggestedFileName);

    string? PickFolder(string description);

    string? PickOpenFile(string title, string filter);

    bool PromptCertificateError(string uri, string errorMessage);

    bool PromptPermission(CoreWebView2PermissionKind kind, string origin);
}
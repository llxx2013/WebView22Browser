using Microsoft.Web.WebView2.Core;
using WebView22Browser.App.Services;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeDialogService : IDialogService
{
    public void ShowError(string message, string title) { }

    public void ShowWarning(string message, string title) { }

    public void ShowInfo(string message, string title) { }

    public bool Confirm(string message, string title) => true;

    public string? PromptSaveFile(string suggestedFileName) => null;

    public string? PickFolder(string description) => null;

    public string? PickOpenFile(string title, string filter) => null;

    public bool PromptCertificateError(string uri, string errorMessage) => false;

    public bool PromptPermission(CoreWebView2PermissionKind kind, string origin) => false;
}

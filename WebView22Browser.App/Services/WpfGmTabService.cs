namespace WebView22Browser.App.Services;

public sealed class WpfGmTabService : IGmTabService
{
    private Action<string, bool>? _openInNewTab;

    public void SetOpenHandler(Action<string, bool> openInNewTab) => _openInNewTab = openInNewTab;

    public void OpenInNewTab(string url, bool activate = true)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _openInNewTab?.Invoke(url, activate);
    }
}
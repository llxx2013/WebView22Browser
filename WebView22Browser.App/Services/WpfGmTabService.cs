namespace WebView22Browser.App.Services;

public sealed class WpfGmTabService : IGmTabService
{
    private Action<string>? _openInNewTab;

    public void SetOpenHandler(Action<string> openInNewTab) => _openInNewTab = openInNewTab;

    public void OpenInNewTab(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        _openInNewTab?.Invoke(url);
    }
}
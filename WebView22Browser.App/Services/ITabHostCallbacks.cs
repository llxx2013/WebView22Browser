using WebView22Browser.App.ViewModels;

namespace WebView22Browser.App.Services;

/// <summary>
/// Main-window coordination callbacks for tab hosts (replaces Service Locator on <see cref="Controls.TabWebViewHost"/>).
/// </summary>
public interface ITabHostCallbacks
{
    void NotifyTabReadyChanged();

    void UpdateWindowTitle();

    void RefreshScriptCommands(BrowserTabViewModel tab);

    void HandleNavigation(string uri, BrowserTabViewModel sourceTab, bool openInNewTab);
}
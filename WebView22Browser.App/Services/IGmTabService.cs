namespace WebView22Browser.App.Services;

public interface IGmTabService
{
    void OpenInNewTab(string url, bool activate = true);
}
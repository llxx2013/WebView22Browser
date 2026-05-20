namespace WebView22Browser.App.Services;

public interface IDesktopService
{
    void ShowInFolder(string filePath);

    void OpenFile(string filePath);
}
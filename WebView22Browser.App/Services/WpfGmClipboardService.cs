using System.Windows;

namespace WebView22Browser.App.Services;

public sealed class WpfGmClipboardService : IGmClipboardService
{
    public void SetText(string text)
    {
        if (Application.Current?.Dispatcher == null)
        {
            Clipboard.SetText(text);
            return;
        }

        if (Application.Current.Dispatcher.CheckAccess())
            Clipboard.SetText(text);
        else
            Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(text));
    }
}

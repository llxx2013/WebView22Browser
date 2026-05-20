using System.Windows;
using System.Windows.Threading;

using WebView22Browser.App.ViewModels;

namespace WebView22Browser.App.Services;

public sealed class WpfBrowserStatusReporter : IBrowserStatusReporter
{
    private readonly Lazy<MainViewModel> _mainViewModel;

    public WpfBrowserStatusReporter(Lazy<MainViewModel> mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public void Report(string message)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            _mainViewModel.Value.StatusMessage = message;
            return;
        }

        dispatcher.BeginInvoke(
            () => _mainViewModel.Value.StatusMessage = message,
            DispatcherPriority.Background);
    }
}
using System.Windows.Threading;

namespace WebView22Browser.App.Services;

public sealed class WpfUiThreadMarshaller : IUiThreadMarshaller
{
    public bool IsOnUiThread
    {
        get
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            return dispatcher == null || dispatcher.CheckAccess();
        }
    }

    public void Invoke(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }
}
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public interface ISystemPressureMonitor : IDisposable
{
    SystemPressureLevel Current { get; }

    event EventHandler? Changed;

    void Start();

    void Stop();
}

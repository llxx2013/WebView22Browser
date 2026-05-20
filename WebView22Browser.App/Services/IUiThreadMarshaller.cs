namespace WebView22Browser.App.Services;

/// <summary>
/// Marshals work to the UI thread without referencing WPF types at call sites (test-friendly).
/// </summary>
public interface IUiThreadMarshaller
{
    bool IsOnUiThread { get; }

    void Invoke(Action action);
}
namespace WebView22Browser.App.Services;

/// <summary>
/// Runs callbacks on the calling thread (for unit tests and headless hosts).
/// </summary>
public sealed class ImmediateUiThreadMarshaller : IUiThreadMarshaller
{
    public bool IsOnUiThread => true;

    public void Invoke(Action action) => action();
}
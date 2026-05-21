using System.Diagnostics;

namespace WebView22Browser.Core.Async;

/// <summary>
/// Runs fire-and-forget work from void event handlers while observing task faults.
/// </summary>
public static class FireAndForget
{
    public static void Run(Func<Task> work, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(work);
        _ = ObserveAsync(work, onError);
    }

    private static async Task ObserveAsync(Func<Task> work, Action<Exception>? onError)
    {
        try
        {
            await work().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            if (onError != null)
                onError(ex);
            else
                Debug.WriteLine($"[FireAndForget] Unhandled: {ex}");
        }
    }
}
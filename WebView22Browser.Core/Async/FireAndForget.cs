using System.Diagnostics;

namespace WebView22Browser.Core.Async;

/// <summary>
/// Runs fire-and-forget work from void event handlers while observing task faults.
/// The observer does not resume on the caller's <see cref="SynchronizationContext"/>; UI
/// affinity is preserved by <paramref name="work"/> itself when it awaits without
/// <c>ConfigureAwait(false)</c>.
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
            // Observer continuation only logs/handles faults — no caller awaits this task.
            // Avoid posting back to a WPF Dispatcher that may be busy or disposed during shutdown.
            await work().ConfigureAwait(false);
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
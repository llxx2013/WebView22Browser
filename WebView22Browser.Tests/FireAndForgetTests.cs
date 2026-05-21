using WebView22Browser.Core.Async;

namespace WebView22Browser.Tests;

public sealed class FireAndForgetTests
{
    [Fact]
    public async Task Run_CompletesSuccessfully()
    {
        var tcs = new TaskCompletionSource();

        FireAndForget.Run(async () =>
        {
            await Task.Yield();
            tcs.SetResult();
        });

        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Run_InvokesOnErrorWhenTaskFaults()
    {
        var expected = new InvalidOperationException("boom");
        var tcs = new TaskCompletionSource<Exception>();

        FireAndForget.Run(
            () => Task.FromException(expected),
            ex => tcs.TrySetResult(ex));

        var observed = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Same(expected, observed);
    }

    [Fact]
    public async Task Run_DoesNotThrowWhenOnErrorOmitted()
    {
        var tcs = new TaskCompletionSource();

        FireAndForget.Run(() => Task.FromException(new InvalidOperationException("boom")));

        await Task.Delay(50);
        tcs.SetResult();
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(2));
    }
}

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeTabWebViewHost : ITabWebViewHost
{
    private readonly List<string> _actions = new();

    public FakeTabWebViewHost(BrowserTabViewModel tab)
    {
        Tab = tab;
    }

    public BrowserTabViewModel? Tab { get; }

    public CoreWebView2? CoreWebView2 => null;

    public IReadOnlyList<string> Actions => _actions;

    public void Shutdown() => _actions.Add(nameof(Shutdown));

    public Task ReduceMemoryAsync()
    {
        _actions.Add(nameof(ReduceMemoryAsync));
        return Task.CompletedTask;
    }

    public Task SuspendLightAsync()
    {
        _actions.Add(nameof(SuspendLightAsync));
        return Task.CompletedTask;
    }

    public Task SuspendAsync()
    {
        _actions.Add(nameof(SuspendAsync));
        return Task.CompletedTask;
    }

    public Task WakeAsync()
    {
        _actions.Add(nameof(WakeAsync));
        return Task.CompletedTask;
    }

    public TabNavigationSnapshot? TryGetHistorySnapshot() => null;
}
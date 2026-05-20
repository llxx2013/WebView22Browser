using WebView22Browser.App.Services;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeRuntimeBrowserSettingsApplier : IRuntimeBrowserSettingsApplier
{
    public int ApplyCount { get; private set; }

    public void ApplyRuntimeSettings() => ApplyCount++;
}
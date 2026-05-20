using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public async Task SaveAsync_ValidInput_SavesToStoreAndAppliesSettings()
    {
        var options = CreateDefaultOptions();
        var store = new TrackingUserSettingsStore();
        var applier = new FakeRuntimeBrowserSettingsApplier();
        var sut = CreateSut(options, store, applier);

        sut.HomeUrl = "https://custom.example";
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(1, store.SaveCount);
        Assert.Equal("https://custom.example", store.LastSaved?.HomeUrl);
        Assert.Equal("https://custom.example", options.HomeUrl);
        Assert.Equal(1, applier.ApplyCount);
        Assert.Equal("设置已保存", sut.StatusMessage);
    }

    [Fact]
    public async Task SaveAsync_InvalidInput_ShowsErrorAndDoesNotSave()
    {
        var options = CreateDefaultOptions();
        var store = new TrackingUserSettingsStore();
        var dialog = new RecordingDialogService();
        var sut = CreateSut(options, store, new FakeRuntimeBrowserSettingsApplier(), dialog);

        sut.HomeUrl = "   ";
        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal(0, store.SaveCount);
        Assert.NotNull(dialog.LastError);
        Assert.Null(sut.StatusMessage);
    }

    [Fact]
    public async Task ResetToDefaultsAsync_ClearsStoreAndReloadsDefaults()
    {
        var appConfig = new BrowserAppConfig
        {
            HomeUrl = "https://factory.example",
            SearchUrlTemplate = "https://factory.example/search?q={0}"
        };
        var options = BrowserOptionsLoader.Load(appConfig, new BrowserSettingsDto { HomeUrl = "https://user.example" });
        var store = new TrackingUserSettingsStore();
        var applier = new FakeRuntimeBrowserSettingsApplier();
        var dialog = new RecordingDialogService { ConfirmResult = true };
        var sut = CreateSut(options, store, applier, dialog, appConfig);

        await sut.ResetToDefaultsCommand.ExecuteAsync(null);

        Assert.Equal(1, store.ClearCount);
        Assert.Equal(1, store.SaveCount);
        Assert.Equal("https://factory.example", options.HomeUrl);
        Assert.Equal("已恢复默认设置", sut.StatusMessage);
    }

    [Fact]
    public void ClosePageCommand_SetsIsPageOpenFalse()
    {
        var sut = CreateSut(CreateDefaultOptions(), new TrackingUserSettingsStore(), new FakeRuntimeBrowserSettingsApplier());
        sut.IsPageOpen = true;

        sut.ClosePageCommand.Execute(null);

        Assert.False(sut.IsPageOpen);
    }

    [Fact]
    public void OpenDataPath_NonExistent_ShowsWarningWithoutCrash()
    {
        var dialog = new RecordingDialogService();
        var sut = CreateSut(CreateDefaultOptions(), new TrackingUserSettingsStore(), new FakeRuntimeBrowserSettingsApplier(), dialog);
        var missing = new SettingsPathItemViewModel("测试", @"C:\nonexistent-wv2-path-xyz", isDirectory: true);

        sut.OpenDataPathCommand.Execute(missing);

        Assert.NotNull(dialog.LastWarning);
    }

    [Fact]
    public void OnPageOpen_ClearsStatusMessage()
    {
        var sut = CreateSut(CreateDefaultOptions(), new TrackingUserSettingsStore(), new FakeRuntimeBrowserSettingsApplier());
        sut.StatusMessage = "设置已保存";

        sut.IsPageOpen = true;

        Assert.Null(sut.StatusMessage);
    }

    private static BrowserOptions CreateDefaultOptions() =>
        BrowserOptionsLoader.Load(
            new BrowserAppConfig { HomeUrl = "https://www.bing.com", SearchUrlTemplate = "https://www.bing.com/search?q={0}" },
            null);

    private static SettingsViewModel CreateSut(
        BrowserOptions options,
        IUserSettingsStore store,
        IRuntimeBrowserSettingsApplier applier,
        IDialogService? dialog = null,
        BrowserAppConfig? appConfig = null)
    {
        var browsingStore = new FakeBrowsingHistoryStore();
        return new SettingsViewModel(
            options,
            appConfig ?? new BrowserAppConfig { HomeUrl = options.HomeUrl, SearchUrlTemplate = options.SearchUrlTemplate },
            store,
            dialog ?? new FakeDialogService(),
            applier,
            new FakeDownloadHistoryStore(),
            browsingStore,
            new BrowsingHistoryService(browsingStore, options));
    }

    private sealed class TrackingUserSettingsStore : IUserSettingsStore
    {
        public BrowserSettingsDto? Current { get; private set; }

        public string FilePath { get; } = Path.Combine(Path.GetTempPath(), "tracking-user-settings.json");

        public int SaveCount { get; private set; }

        public int ClearCount { get; private set; }

        public BrowserSettingsDto? LastSaved { get; private set; }

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveAsync(BrowserSettingsDto settings, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            LastSaved = settings;
            Current = settings;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            ClearCount++;
            Current = null;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDialogService : IDialogService
    {
        public string? LastError { get; private set; }

        public string? LastWarning { get; private set; }

        public bool ConfirmResult { get; set; } = true;

        public void ShowError(string message, string title) => LastError = message;

        public void ShowWarning(string message, string title) => LastWarning = message;

        public void ShowInfo(string message, string title) { }

        public bool Confirm(string message, string title) => ConfirmResult;

        public string? PromptSaveFile(string suggestedFileName) => null;

        public string? PickFolder(string description) => null;

        public string? PickOpenFile(string title, string filter) => null;

        public bool PromptCertificateError(string uri, string errorMessage) => false;

        public bool PromptPermission(CoreWebView2PermissionKind kind, string origin) => false;
    }
}
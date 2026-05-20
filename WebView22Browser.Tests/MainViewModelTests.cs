using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class MainViewModelTests
{
    private static MainViewModel CreateSut(BrowserOptions? options = null, ITabSessionStore? sessionStore = null)
    {
        var effectiveOptions = options ?? new BrowserOptions { HomeUrl = "https://www.bing.com", RestoreLastSession = false };
        var tabHostService = new TabHostService();
        var userScriptStore = new JsonUserScriptStore(Path.Combine(Path.GetTempPath(), $"userscript-vm-{Guid.NewGuid()}.json"));
        var gmStore = new JsonGmStorageStore(
            Path.Combine(Path.GetTempPath(), $"gm-vm-{Guid.NewGuid():N}"),
            new GmStorageQuota());
        var dependencyResolver = new FakeUserScriptDependencyResolver();
        var userScriptService = new UserScriptService(
            userScriptStore,
            new UserScriptBootstrapBuilder(),
            TestServiceFactory.CreateUserScriptBridge(gmStore),
            gmStore,
            tabHostService,
            dependencyResolver);
        var extensionService = new BrowserExtensionService(
            new JsonExtensionSourceStore(Path.Combine(Path.GetTempPath(), $"ext-vm-{Guid.NewGuid()}.json")),
            new FakeDialogService());
        var conflictService = new UserScriptExtensionConflictService(extensionService, new ExtensionManifestReader());
        var importService = new UserScriptImportService(new FakeDialogService(), conflictService, dependencyResolver);
        sessionStore ??= new JsonTabSessionStore(Path.Combine(Path.GetTempPath(), $"tabs-vm-{Guid.NewGuid():N}.json"));

        MainViewModel? mainVm = null;
        var userScripts = new UserScriptsViewModel(
            userScriptStore,
            userScriptService,
            importService,
            conflictService,
            dependencyResolver,
            effectiveOptions,
            new FakeDialogService(),
            new Lazy<MainViewModel>(() => mainVm!));

        mainVm = new MainViewModel(
            effectiveOptions,
            new NavigationService(new BrowserOptions
            {
                HomeUrl = effectiveOptions.HomeUrl,
                SearchUrlTemplate = "https://www.bing.com/search?q={0}"
            }),
            sessionStore,
            new FavoritesViewModel(new FakeFavoritesStore()),
            new ExtensionsViewModel(extensionService, new FakeDialogService()),
            userScripts,
            new UserScriptCommandsViewModel(
                TestServiceFactory.CreateMenuCommandRegistry(),
                tabHostService,
                userScriptStore,
                new ImmediateUiThreadMarshaller()),
            userScriptService,
            new DownloadsViewModel(new FakeDownloadHistoryStore(), effectiveOptions),
            new HistoryViewModel(new FakeBrowsingHistoryStore(), new BrowsingHistoryService(new FakeBrowsingHistoryStore(), effectiveOptions)),
            CreateSettingsViewModel(effectiveOptions));

        return mainVm;
    }

    private static SettingsViewModel CreateSettingsViewModel(BrowserOptions options)
    {
        var browsingStore = new FakeBrowsingHistoryStore();
        return new SettingsViewModel(
            options,
            new BrowserAppConfig { HomeUrl = options.HomeUrl, SearchUrlTemplate = options.SearchUrlTemplate },
            new FakeUserSettingsStore(),
            new FakeDialogService(),
            new FakeRuntimeBrowserSettingsApplier(),
            new FakeDownloadHistoryStore(),
            browsingStore,
            new BrowsingHistoryService(browsingStore, options));
    }

    private static async Task<JsonTabSessionStore> CreateSessionStoreWithAsync(TabSessionFile session)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tabs-vm-{Guid.NewGuid():N}.json");
        var store = new JsonTabSessionStore(path);
        store.Update(session);
        await store.FlushAsync();
        return store;
    }

    [Fact]
    public void OpenNewTab_AddsTabAndSelectsIt()
    {
        var vm = CreateSut();
        var tab = vm.OpenNewTab("https://example.com");

        Assert.Single(vm.Tabs);
        Assert.Same(tab, vm.SelectedTab);
        Assert.Equal("https://example.com", tab.InitialUri);
    }

    [Fact]
    public void CloseTab_RemovesTab()
    {
        var vm = CreateSut();
        vm.OpenNewTab("https://a.com");
        var second = vm.OpenNewTab("https://b.com");

        vm.CloseTabCommand.Execute(second);

        Assert.Single(vm.Tabs);
        Assert.NotNull(vm.SelectedTab);
        Assert.NotSame(second, vm.SelectedTab);
    }

    [Fact]
    public void HandleNavigation_OpenInNewTab_AddsTab()
    {
        var vm = CreateSut();
        var source = vm.OpenNewTab("https://a.com");

        vm.HandleNavigation("https://example.com", source, openInNewTab: true);

        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal("https://example.com", vm.SelectedTab?.InitialUri);
    }

    [Fact]
    public void HandleNavigation_SameTab_InvokesNavigateOnSourceTab()
    {
        var vm = CreateSut();
        var source = vm.OpenNewTab("https://a.com");

        string? navigated = null;
        source.NavigateRequested += uri => navigated = uri;
        vm.HandleNavigation("https://example.com", source, openInNewTab: false);

        Assert.Single(vm.Tabs);
        Assert.Equal("https://example.com", navigated);
    }

    [Fact]
    public void CloseTab_WhenClosingSelectedFirstTab_SelectsSecond()
    {
        var vm = CreateSut();
        var first = vm.OpenNewTab("https://a.com");
        var second = vm.OpenNewTab("https://b.com");

        vm.CloseTabCommand.Execute(first);

        Assert.Single(vm.Tabs);
        Assert.Same(second, vm.SelectedTab);
    }

    [Fact]
    public void CloseTab_WhenClosingSelectedSecondTab_SelectsFirst()
    {
        var vm = CreateSut();
        var first = vm.OpenNewTab("https://a.com");
        var second = vm.OpenNewTab("https://b.com");

        vm.CloseTabCommand.Execute(second);

        Assert.Single(vm.Tabs);
        Assert.Same(first, vm.SelectedTab);
    }

    [Fact]
    public void CloseTab_WhenLastTab_OpensNewThenRemovesOld()
    {
        var vm = CreateSut();
        var only = vm.OpenNewTab("https://old.com");
        var onlyId = only.TabId;

        vm.CloseTabCommand.Execute(only);

        Assert.Single(vm.Tabs);
        Assert.NotEqual(onlyId, vm.SelectedTab?.TabId);
        Assert.Equal("https://www.bing.com", vm.SelectedTab?.InitialUri);
    }

    [Fact]
    public void CanCloseTab_WhenSingleTab_ReturnsTrue()
    {
        var vm = CreateSut();
        vm.OpenNewTab();

        Assert.True(vm.CloseTabCommand.CanExecute(vm.SelectedTab));
    }

    [Fact]
    public void ShowFindCommand_WhenToolbarDisabled_CannotExecute()
    {
        var vm = CreateSut();
        vm.OpenNewTab();
        vm.IsToolbarEnabled = false;

        Assert.False(vm.ShowFindCommand.CanExecute(null));
    }

    [Fact]
    public void ShowFindCommand_WhenTabSelectedAndToolbarEnabled_CanExecute()
    {
        var vm = CreateSut();
        vm.OpenNewTab();
        vm.IsToolbarEnabled = true;

        Assert.True(vm.ShowFindCommand.CanExecute(null));
    }

    [Fact]
    public void ShowFindCommand_WhenNoSelectedTab_CannotExecute()
    {
        var vm = CreateSut();
        vm.IsToolbarEnabled = true;

        Assert.Null(vm.SelectedTab);
        Assert.False(vm.ShowFindCommand.CanExecute(null));
    }

    [Fact]
    public void NavigateCommand_ResolvesAddressOnSelectedTab()
    {
        var vm = CreateSut();
        var tab = vm.OpenNewTab();
        tab.IsWebViewReady = true;
        vm.IsToolbarEnabled = true;
        tab.Address = "example.com";

        string? navigated = null;
        tab.NavigateRequested += uri => navigated = uri;
        vm.NavigateCommand.Execute(null);

        Assert.Equal("https://example.com/", navigated);
    }

    [Fact]
    public async Task InitializeAsync_RestoresSession_SelectedTabIsNotSleeping()
    {
        var firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var session = new TabSessionFile
        {
            Version = 1,
            SelectedTabId = secondId,
            Tabs =
            [
                new TabSessionSnapshot
                {
                    TabId = firstId,
                    Title = "First",
                    Address = "https://first.example",
                    History = ["https://first.example"],
                    CurrentIndex = 0
                },
                new TabSessionSnapshot
                {
                    TabId = secondId,
                    Title = "Second",
                    Address = "https://second.example",
                    History = ["https://second.example"],
                    CurrentIndex = 0
                }
            ]
        };

        var sessionStore = await CreateSessionStoreWithAsync(session);
        var options = new BrowserOptions { HomeUrl = "https://www.bing.com", RestoreLastSession = true };
        var vm = CreateSut(options, sessionStore);

        var restored = await vm.InitializeAsync();

        Assert.True(restored);
        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal(secondId, vm.SelectedTab?.TabId);
        Assert.False(vm.SelectedTab!.IsSleeping);
        Assert.True(vm.Tabs.First(t => t.TabId == firstId).IsSleeping);
    }

    [Fact]
    public async Task InitializeAsync_RestoresSession_FallsBackToFirstTabWhenSelectedIdMissing()
    {
        var firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var session = new TabSessionFile
        {
            Version = 1,
            SelectedTabId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Tabs =
            [
                new TabSessionSnapshot
                {
                    TabId = firstId,
                    Title = "First",
                    Address = "https://first.example",
                    History = ["https://first.example"],
                    CurrentIndex = 0
                },
                new TabSessionSnapshot
                {
                    TabId = secondId,
                    Title = "Second",
                    Address = "https://second.example",
                    History = ["https://second.example"],
                    CurrentIndex = 0
                }
            ]
        };

        var sessionStore = await CreateSessionStoreWithAsync(session);
        var options = new BrowserOptions { HomeUrl = "https://www.bing.com", RestoreLastSession = true };
        var vm = CreateSut(options, sessionStore);

        var restored = await vm.InitializeAsync();

        Assert.True(restored);
        Assert.Equal(2, vm.Tabs.Count);
        Assert.Equal(firstId, vm.SelectedTab?.TabId);
        Assert.False(vm.Tabs[0].IsSleeping);
        Assert.True(vm.Tabs[1].IsSleeping);
    }
}
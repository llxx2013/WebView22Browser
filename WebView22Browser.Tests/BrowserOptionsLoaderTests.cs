using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowserOptionsLoaderTests
{
    [Fact]
    public void Load_AppConfigOnly_UsesAppValues()
    {
        var appConfig = new BrowserAppConfig
        {
            HomeUrl = "https://example.com",
            TabSleepTimeoutMinutes = 10,
            RestoreLastSession = false
        };

        var options = BrowserOptionsLoader.Load(appConfig, null);

        Assert.Equal("https://example.com", options.HomeUrl);
        Assert.Equal(10, options.TabSleepTimeoutMinutes);
        Assert.False(options.RestoreLastSession);
    }

    [Fact]
    public void Load_UserSettingsOverridesAppConfig()
    {
        var appConfig = new BrowserAppConfig { HomeUrl = "https://a.com" };
        var user = new BrowserSettingsDto { HomeUrl = "https://b.com", TabSleepTimeoutMinutes = 0 };

        var options = BrowserOptionsLoader.Load(appConfig, user);

        Assert.Equal("https://b.com", options.HomeUrl);
        Assert.Equal(0, options.TabSleepTimeoutMinutes);
    }

    [Fact]
    public void DefaultsFromAppConfig_MatchesLoadWithoutUser()
    {
        var appConfig = new BrowserAppConfig
        {
            SearchUrlTemplate = "https://search.test?q={0}",
            PressureHighCpuPercent = 90
        };

        var fromDefaults = BrowserOptionsLoader.DefaultsFromAppConfig(appConfig);
        var fromLoad = BrowserOptionsLoader.FromOptions(BrowserOptionsLoader.Load(appConfig, null));

        Assert.Equal(fromLoad.SearchUrlTemplate, fromDefaults.SearchUrlTemplate);
        Assert.Equal(fromLoad.PressureHighCpuPercent, fromDefaults.PressureHighCpuPercent);
    }
}
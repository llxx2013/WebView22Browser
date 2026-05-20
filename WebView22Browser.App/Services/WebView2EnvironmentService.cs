using System.IO;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core;

namespace WebView22Browser.App.Services;

public sealed class WebView2EnvironmentService
{
    private readonly BrowserOptions _options;
    private readonly Lazy<Task<CoreWebView2Environment>> _environment;

    public WebView2EnvironmentService(BrowserOptions options)
    {
        _options = options;
        _environment = new Lazy<Task<CoreWebView2Environment>>(CreateEnvironmentAsync);
    }

    public string UserDataFolder => _options.GetUserDataFolder();

    public Task<CoreWebView2Environment> GetAsync() => _environment.Value;

    private async Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        var folder = UserDataFolder;
        Directory.CreateDirectory(folder);
        var options = new CoreWebView2EnvironmentOptions
        {
            AreBrowserExtensionsEnabled = true
        };
        return await CoreWebView2Environment.CreateAsync(null, folder, options);
    }
}
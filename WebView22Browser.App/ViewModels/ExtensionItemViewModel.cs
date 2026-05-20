using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Core;

namespace WebView22Browser.App.ViewModels;

public partial class ExtensionItemViewModel : ObservableObject
{
    public ExtensionItemViewModel(CoreWebView2BrowserExtension extension, string? sourcePath)
    {
        Extension = extension;
        Id = extension.Id;
        Name = extension.Name;
        IsEnabled = extension.IsEnabled;
        SourcePath = sourcePath ?? string.Empty;
    }

    internal CoreWebView2BrowserExtension Extension { get; }

    public string Id { get; }

    public string Name { get; }

    [ObservableProperty]
    private bool _isEnabled;

    public string SourcePath { get; }
}

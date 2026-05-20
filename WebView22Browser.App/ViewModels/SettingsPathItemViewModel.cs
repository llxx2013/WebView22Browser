namespace WebView22Browser.App.ViewModels;

public sealed class SettingsPathItemViewModel
{
    public SettingsPathItemViewModel(string label, string path, bool isDirectory)
    {
        Label = label;
        Path = path;
        IsDirectory = isDirectory;
    }

    public string Label { get; }

    public string Path { get; }

    public bool IsDirectory { get; }
}

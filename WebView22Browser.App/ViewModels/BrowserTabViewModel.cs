using CommunityToolkit.Mvvm.ComponentModel;

namespace WebView22Browser.App.ViewModels;

public partial class BrowserTabViewModel : ObservableObject
{
    public Guid TabId { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _title = "新标签页";

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingStatus = string.Empty;

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isWebViewReady;

    [ObservableProperty]
    private bool _isSleeping;

    [ObservableProperty]
    private bool _isMemoryReduced;

    [ObservableProperty]
    private bool _isLightSuspended;

    [ObservableProperty]
    private bool _isPlayingAudio;

    [ObservableProperty]
    private int _activeDownloadCount;

    public DateTime LastActiveUtc { get; private set; } = DateTime.UtcNow;

    public IReadOnlyList<string>? FrozenHistoryEntries { get; set; }

    public int FrozenHistoryIndex { get; set; } = -1;

    public bool HasActiveDownloads => ActiveDownloadCount > 0;

    public void ClearFrozenHistory()
    {
        FrozenHistoryEntries = null;
        FrozenHistoryIndex = -1;
    }

    public bool SuppressNavigationError { get; set; }

    public string? InitialUri { get; init; }

    public event Action<string>? NavigateRequested;

    public event Action? GoBackRequested;

    public event Action? GoForwardRequested;

    public event Action? ReloadRequested;

    public event Action? StopRequested;

    public event Action? OpenDevToolsRequested;

    public void RequestNavigate(string uri) => NavigateRequested?.Invoke(uri);

    public void RequestGoBack() => GoBackRequested?.Invoke();

    public void RequestGoForward() => GoForwardRequested?.Invoke();

    public void RequestReload() => ReloadRequested?.Invoke();

    public void RequestStop() => StopRequested?.Invoke();

    public void RequestOpenDevTools() => OpenDevToolsRequested?.Invoke();

    public void TouchActivity() => LastActiveUtc = DateTime.UtcNow;

    public void SetLastActiveUtc(DateTime utc) => LastActiveUtc = utc;
}

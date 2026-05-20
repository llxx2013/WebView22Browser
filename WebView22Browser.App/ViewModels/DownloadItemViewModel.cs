using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.ViewModels;

public partial class DownloadItemViewModel : ObservableObject
{
    private Action? _cancelAction;
    private Action? _pauseAction;
    private Action? _resumeAction;

    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _sourceUrl = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private DownloadState _state;

    [ObservableProperty]
    private double? _progress;

    [ObservableProperty]
    private bool _isProgressIndeterminate;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private string _speedText = string.Empty;

    [ObservableProperty]
    private bool _canPause;

    [ObservableProperty]
    private bool _canResume;

    [ObservableProperty]
    private long _bytesReceived;

    [ObservableProperty]
    private long _totalBytes;

    public bool IsActive => State == DownloadState.InProgress;

    public bool CanShowInFolder =>
        State == DownloadState.Completed &&
        !string.IsNullOrWhiteSpace(FullPath) &&
        File.Exists(FullPath);

    public bool CanOpen => CanShowInFolder;

    public void BindOperations(Action? cancel, Action? pause, Action? resume)
    {
        _cancelAction = cancel;
        _pauseAction = pause;
        _resumeAction = resume;
    }

    public void ClearOperations() => BindOperations(null, null, null);

    public void UpdateProgress(long bytesReceived, long totalBytes, long bytesPerSecond)
    {
        BytesReceived = bytesReceived;
        TotalBytes = totalBytes;
        Progress = DownloadProgressFormatter.GetProgressValue(bytesReceived, totalBytes);
        IsProgressIndeterminate = State == DownloadState.InProgress && totalBytes <= 0;
        ProgressText = DownloadProgressFormatter.FormatProgressText(bytesReceived, totalBytes);
        SpeedText = DownloadProgressFormatter.FormatSpeed(bytesPerSecond);
        OnPropertyChanged(nameof(CanShowInFolder));
        OnPropertyChanged(nameof(CanOpen));
    }

    public void SetState(DownloadState state)
    {
        State = state;
        IsProgressIndeterminate = false;
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(CanShowInFolder));
        OnPropertyChanged(nameof(CanOpen));
        CancelCommand.NotifyCanExecuteChanged();
        PauseCommand.NotifyCanExecuteChanged();
        ResumeCommand.NotifyCanExecuteChanged();
        ShowInFolderCommand.NotifyCanExecuteChanged();
        OpenCommand.NotifyCanExecuteChanged();
    }

    partial void OnStateChanged(DownloadState value)
    {
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(CanShowInFolder));
        OnPropertyChanged(nameof(CanOpen));
    }

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void Cancel() => _cancelAction?.Invoke();

    [RelayCommand(CanExecute = nameof(CanPauseExecute))]
    private void Pause() => _pauseAction?.Invoke();

    [RelayCommand(CanExecute = nameof(CanResumeExecute))]
    private void Resume() => _resumeAction?.Invoke();

    [RelayCommand(CanExecute = nameof(CanShowInFolder))]
    private void ShowInFolder() => ShowInFolderRequested?.Invoke(FullPath);

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open() => OpenRequested?.Invoke(FullPath);

    public event Action<string>? ShowInFolderRequested;

    public event Action<string>? OpenRequested;

    public event Action<DownloadItemViewModel>? RemoveFromHistoryRequested;

    private bool CanCancel() => State == DownloadState.InProgress;

    private bool CanPauseExecute() => State == DownloadState.InProgress && CanPause;

    private bool CanResumeExecute() => State == DownloadState.InProgress && CanResume;

    [RelayCommand]
    private void RemoveFromHistory() => RemoveFromHistoryRequested?.Invoke(this);

    public DownloadHistoryEntry ToHistoryEntry() => new()
    {
        Id = Id,
        FileName = DisplayName,
        SourceUrl = SourceUrl,
        FullPath = FullPath,
        StartedAtUtc = StartedAtUtc,
        CompletedAtUtc = CompletedAtUtc,
        State = State,
        BytesReceived = BytesReceived,
        TotalBytes = TotalBytes
    };

    public static DownloadItemViewModel FromHistoryEntry(DownloadHistoryEntry entry)
    {
        var vm = new DownloadItemViewModel
        {
            Id = entry.Id,
            DisplayName = entry.FileName,
            SourceUrl = entry.SourceUrl,
            FullPath = entry.FullPath,
            State = entry.State == DownloadState.InProgress ? DownloadState.Interrupted : entry.State,
            BytesReceived = entry.BytesReceived,
            TotalBytes = entry.TotalBytes,
            StartedAtUtc = entry.StartedAtUtc,
            CompletedAtUtc = entry.CompletedAtUtc
        };
        vm.Progress = DownloadProgressFormatter.GetProgressValue(entry.BytesReceived, entry.TotalBytes);
        vm.ProgressText = DownloadProgressFormatter.FormatProgressText(entry.BytesReceived, entry.TotalBytes);
        return vm;
    }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }
}

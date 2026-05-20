using System.IO;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public sealed class WebView2DownloadService : IDownloadService
{
    private readonly IDialogService _dialogService;
    private readonly DownloadsViewModel _downloads;
    private readonly IDesktopService _desktopService;
    private readonly List<DownloadSession> _sessions = [];

    public WebView2DownloadService(
        IDialogService dialogService,
        DownloadsViewModel downloads,
        IDesktopService desktopService)
    {
        _dialogService = dialogService;
        _downloads = downloads;
        _desktopService = desktopService;

        _downloads.ShowInFolderHandler = _desktopService.ShowInFolder;
        _downloads.OpenFileHandler = _desktopService.OpenFile;
    }

    public void HandleDownloadStarting(CoreWebView2DownloadStartingEventArgs e, BrowserTabViewModel tab)
    {
        var path = _dialogService.PromptSaveFile(e.DownloadOperation.ResultFilePath ?? "download");
        if (path == null)
        {
            e.Cancel = true;
            return;
        }

        e.ResultFilePath = path;
        tab.ActiveDownloadCount++;
        tab.TouchActivity();

        var item = new DownloadItemViewModel
        {
            DisplayName = Path.GetFileName(path),
            SourceUrl = e.DownloadOperation.Uri ?? string.Empty,
            FullPath = path,
            State = DownloadState.InProgress
        };

        item.ShowInFolderRequested += OnShowInFolder;
        item.OpenRequested += OnOpenFile;

        var session = new DownloadSession(item, e.DownloadOperation, tab, this);
        _sessions.Add(session);
        item.BindOperations(() => session.Cancel(), () => session.Pause(), () => session.Resume());
        _downloads.AddActiveItem(item);
        session.Start();
    }

    public void DetachTab(BrowserTabViewModel tab)
    {
        foreach (var session in _sessions.Where(s => s.Tab == tab).ToList())
            session.Cancel(userInitiated: true);
    }

    private void OnShowInFolder(string path) => _desktopService.ShowInFolder(path);

    private void OnOpenFile(string path) => _desktopService.OpenFile(path);

    private void RemoveSession(DownloadSession session) => _sessions.Remove(session);

    private sealed class DownloadSession
    {
        private readonly DownloadItemViewModel _item;
        private readonly CoreWebView2DownloadOperation _operation;
        private readonly BrowserTabViewModel _tab;
        private readonly WebView2DownloadService _service;
        private bool _userCancelled;
        private bool _isPaused;
        private long _lastBytes;
        private DateTime _lastSampleUtc = DateTime.UtcNow;

        public DownloadSession(
            DownloadItemViewModel item,
            CoreWebView2DownloadOperation operation,
            BrowserTabViewModel tab,
            WebView2DownloadService service)
        {
            _item = item;
            _operation = operation;
            _tab = tab;
            _service = service;
        }

        public BrowserTabViewModel Tab => _tab;

        public void Start()
        {
            _operation.BytesReceivedChanged += OnBytesReceivedChanged;
            _operation.StateChanged += OnStateChanged;
            UpdateProgress();
            UpdatePauseResume();
        }

        public void Cancel(bool userInitiated = true)
        {
            if (userInitiated)
                _userCancelled = true;

            if (_operation.State == CoreWebView2DownloadState.InProgress)
                _operation.Cancel();
        }

        public void Pause()
        {
            if (_operation.State == CoreWebView2DownloadState.InProgress && _operation.CanResume)
            {
                _operation.Pause();
                _isPaused = true;
            }

            UpdatePauseResume();
        }

        public void Resume()
        {
            if (_operation.State == CoreWebView2DownloadState.InProgress && _isPaused)
            {
                _operation.Resume();
                _isPaused = false;
            }

            UpdatePauseResume();
        }

        private void OnBytesReceivedChanged(object? sender, object e) => UpdateProgress();

        private void OnStateChanged(object? sender, object e)
        {
            UpdateProgress();
            UpdatePauseResume();

            if (_operation.State == CoreWebView2DownloadState.InProgress)
                return;

            Unsubscribe();
            _item.CompletedAtUtc = DateTime.UtcNow;

            var state = _operation.State switch
            {
                CoreWebView2DownloadState.Completed => DownloadState.Completed,
                CoreWebView2DownloadState.Interrupted when _userCancelled => DownloadState.Cancelled,
                CoreWebView2DownloadState.Interrupted => DownloadState.Interrupted,
                _ => DownloadState.Interrupted
            };

            _item.SetState(state);
            DecrementTabCount();
            _ = _service._downloads.MoveToHistoryAsync(_item);
            _service.RemoveSession(this);
        }

        private void UpdateProgress()
        {
            var bytes = (long)_operation.BytesReceived;
            var total = _operation.TotalBytesToReceive.HasValue
                ? (long)_operation.TotalBytesToReceive.Value
                : 0L;
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastSampleUtc).TotalSeconds;
            long speed = 0;
            if (elapsed > 0.2)
            {
                speed = (long)Math.Max(0, (bytes - _lastBytes) / elapsed);
                _lastBytes = bytes;
                _lastSampleUtc = now;
            }

            _item.UpdateProgress(bytes, total, speed);
        }

        private void UpdatePauseResume()
        {
            _item.CanPause = _operation.State == CoreWebView2DownloadState.InProgress && _operation.CanResume && !_isPaused;
            _item.CanResume = _operation.State == CoreWebView2DownloadState.InProgress && _isPaused;
            _item.PauseCommand.NotifyCanExecuteChanged();
            _item.ResumeCommand.NotifyCanExecuteChanged();
        }

        private void Unsubscribe()
        {
            _operation.BytesReceivedChanged -= OnBytesReceivedChanged;
            _operation.StateChanged -= OnStateChanged;
        }

        private void DecrementTabCount()
        {
            if (_tab.ActiveDownloadCount > 0)
                _tab.ActiveDownloadCount--;
        }
    }
}
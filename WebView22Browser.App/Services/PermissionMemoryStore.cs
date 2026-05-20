using System.Diagnostics;
using System.IO;
using System.Text.Json;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class PermissionMemoryStore : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;
    private readonly TimeSpan _flushDelay;
    private readonly IBrowserStatusReporter? _statusReporter;
    private Dictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDirty;
    private CancellationTokenSource? _flushCts;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _flushCount;

    public PermissionMemoryStore(
        BrowserOptions options,
        TimeSpan? flushDelay = null,
        IBrowserStatusReporter? statusReporter = null)
    {
        _filePath = Path.Combine(
            options.UserDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "permissions.json");
        _flushDelay = flushDelay ?? TimeSpan.FromSeconds(3);
        _statusReporter = statusReporter;
    }

    public int FlushCount => _flushCount;

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, Dictionary<string, string>>>(stream);
            if (loaded != null)
                _cache = loaded;
        }
        catch (JsonException)
        {
            _cache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool TryGet(string origin, CoreWebView2PermissionKind kind, out CoreWebView2PermissionState state)
    {
        state = CoreWebView2PermissionState.Default;
        if (!_cache.TryGetValue(origin, out var kinds))
            return false;

        if (!kinds.TryGetValue(kind.ToString(), out var value))
            return false;

        if (Enum.TryParse<CoreWebView2PermissionState>(value, out state))
            return state != CoreWebView2PermissionState.Default;

        return false;
    }

    public Task SetAsync(string origin, CoreWebView2PermissionKind kind, CoreWebView2PermissionState state)
    {
        if (!_cache.TryGetValue(origin, out var kinds))
        {
            kinds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _cache[origin] = kinds;
        }

        kinds[kind.ToString()] = state.ToString();
        _isDirty = true;
        ScheduleFlush();
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        if (!_isDirty)
            return;

        await _flushLock.WaitAsync();
        try
        {
            if (!_isDirty)
                return;

            Exception? lastError = null;
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await JsonFileStoreBase.WriteAtomicAsync(_filePath, _cache, JsonOptions);
                    _isDirty = false;
                    _flushCount++;
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
                {
                    lastError = ex;
                    if (attempt == 0)
                        await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    break;
                }
            }

            if (lastError != null)
                ReportFlushFailure(lastError);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    private void ScheduleFlush()
    {
        _flushCts?.Cancel();
        _flushCts = new CancellationTokenSource();
        var token = _flushCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_flushDelay, token);
                await FlushAsync();
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                ReportFlushFailure(ex);
            }
        }, token);
    }

    private void ReportFlushFailure(Exception ex)
    {
        Debug.WriteLine($"PermissionMemoryStore flush failed: {ex}");
        _statusReporter?.Report("权限记忆保存失败，将在下次变更时重试。");
    }

    public async ValueTask DisposeAsync()
    {
        _flushCts?.Cancel();
        await FlushAsync();
        _flushCts?.Dispose();
        _flushLock.Dispose();
    }
}
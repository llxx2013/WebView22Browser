using System.IO;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using WebView22Browser.Core;

namespace WebView22Browser.App.Services;

public sealed class PermissionMemoryStore : IAsyncDisposable
{
    private readonly string _filePath;
    private readonly TimeSpan _flushDelay;
    private Dictionary<string, Dictionary<string, string>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private bool _isDirty;
    private CancellationTokenSource? _flushCts;
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private int _flushCount;

    public PermissionMemoryStore(BrowserOptions options, TimeSpan? flushDelay = null)
    {
        _filePath = Path.Combine(
            options.UserDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "permissions.json");
        _flushDelay = flushDelay ?? TimeSpan.FromSeconds(3);
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

            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, _cache, new JsonSerializerOptions { WriteIndented = true });
            _isDirty = false;
            _flushCount++;
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
            catch (Exception)
            {
                // Dispose/IO race: leave _isDirty true for a later flush or DisposeAsync
            }
        }, token);
    }

    public async ValueTask DisposeAsync()
    {
        _flushCts?.Cancel();
        await FlushAsync();
        _flushCts?.Dispose();
        _flushLock.Dispose();
    }
}

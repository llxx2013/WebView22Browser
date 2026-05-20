using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonGmStorageStore : IGmStorageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _rootDirectory;
    private readonly GmStorageQuota _quota;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _locks = new();

    public JsonGmStorageStore(string rootDirectory, GmStorageQuota quota)
    {
        _rootDirectory = rootDirectory;
        _quota = quota;
    }

    public async Task<IReadOnlyDictionary<string, JsonElement>> LoadAsync(
        Guid scriptId,
        CancellationToken cancellationToken = default)
    {
        var gate = GetLock(scriptId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadFileAsync(scriptId, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SetValueAsync(
        Guid scriptId,
        string key,
        JsonElement value,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ValidateValueSize(value);

        var gate = GetLock(scriptId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadFileAsync(scriptId, cancellationToken);
            var mutable = data.ToDictionary(static pair => pair.Key, static pair => pair.Value);
            mutable[key] = value.Clone();
            ValidateScriptSize(mutable);
            await WriteFileAsync(scriptId, mutable, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteValueAsync(
        Guid scriptId,
        string key,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);

        var gate = GetLock(scriptId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var data = await ReadFileAsync(scriptId, cancellationToken);
            if (!data.ContainsKey(key))
                return;

            var mutable = data.ToDictionary(static pair => pair.Key, static pair => pair.Value);
            mutable.Remove(key);

            if (mutable.Count == 0)
            {
                var path = GetFilePath(scriptId);
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }

            await WriteFileAsync(scriptId, mutable, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task DeleteScriptAsync(Guid scriptId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var gate = GetLock(scriptId);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var path = GetFilePath(scriptId);
            if (File.Exists(path))
                File.Delete(path);
        }
        finally
        {
            gate.Release();
        }

        // The script is gone; drop its per-script lock to avoid unbounded growth in the
        // dictionary over the lifetime of a long-running session. Re-creation is cheap if
        // the same id is ever re-added.
        if (_locks.TryRemove(scriptId, out var removed))
            removed.Dispose();
    }

    private SemaphoreSlim GetLock(Guid scriptId) =>
        _locks.GetOrAdd(scriptId, static _ => new SemaphoreSlim(1, 1));

    private string GetFilePath(Guid scriptId)
    {
        if (scriptId == Guid.Empty)
            throw new ArgumentException("Script id must not be empty.", nameof(scriptId));

        return Path.Combine(_rootDirectory, $"{scriptId:D}.json");
    }

    private async Task<Dictionary<string, JsonElement>> ReadFileAsync(
        Guid scriptId,
        CancellationToken cancellationToken)
    {
        var path = GetFilePath(scriptId);
        if (!File.Exists(path))
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement>>(
                stream, JsonOptions, cancellationToken);
            return loaded ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
    }

    private async Task WriteFileAsync(
        Guid scriptId,
        Dictionary<string, JsonElement> data,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_rootDirectory);
        var path = GetFilePath(scriptId);
        await JsonFileStoreBase.WriteAtomicAsync(path, data, JsonOptions, cancellationToken: cancellationToken);
    }

    private void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key must not be empty.", nameof(key));

        if (key.Length > _quota.MaxKeyLength)
            throw new GmStorageQuotaExceededException($"GM storage key exceeds {_quota.MaxKeyLength} characters.");
    }

    private void ValidateValueSize(JsonElement value)
    {
        var bytes = Encoding.UTF8.GetByteCount(value.GetRawText());
        if (bytes > _quota.MaxValueBytes)
            throw new GmStorageQuotaExceededException($"GM storage value exceeds {_quota.MaxValueBytes} bytes.");
    }

    private void ValidateScriptSize(Dictionary<string, JsonElement> data)
    {
        var total = data.Sum(static pair => Encoding.UTF8.GetByteCount(pair.Key) + Encoding.UTF8.GetByteCount(pair.Value.GetRawText()));
        if (total > _quota.MaxScriptBytes)
            throw new GmStorageQuotaExceededException($"GM storage for script exceeds {_quota.MaxScriptBytes} bytes.");
    }
}
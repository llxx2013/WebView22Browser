using System.Text.Json;

namespace WebView22Browser.Core.Stores;

/// <summary>
/// Atomic JSON file writes (temp + replace). Optional per-file write serialization via <see cref="WriteLock"/>.
/// </summary>
public abstract class JsonFileStoreBase
{
    private readonly SemaphoreSlim? _writeLock;

    protected JsonFileStoreBase(string filePath, bool serializeWrites = false)
    {
        FilePath = filePath;
        if (serializeWrites)
            _writeLock = new SemaphoreSlim(1, 1);
    }

    protected string FilePath { get; }

    protected SemaphoreSlim? WriteLock => _writeLock;

    protected Task WriteAtomicAsync<T>(
        T value,
        JsonSerializerOptions options,
        CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(FilePath, value, options, _writeLock, cancellationToken);

    public static async Task WriteAtomicAsync<T>(
        string filePath,
        T value,
        JsonSerializerOptions options,
        SemaphoreSlim? writeLock = null,
        CancellationToken cancellationToken = default)
    {
        if (writeLock != null)
            await writeLock.WaitAsync(cancellationToken);

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var tempPath = filePath + ".tmp";
            await using (var stream = File.Create(tempPath))
                await JsonSerializer.SerializeAsync(stream, value, options, cancellationToken);

            File.Move(tempPath, filePath, overwrite: true);
        }
        finally
        {
            writeLock?.Release();
        }
    }
}
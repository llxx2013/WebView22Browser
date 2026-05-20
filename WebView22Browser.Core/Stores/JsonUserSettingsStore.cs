using System.Text.Json;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonUserSettingsStore : IUserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private BrowserSettingsDto? _current;

    public JsonUserSettingsStore()
        : this(GetDefaultFilePath())
    {
    }

    public JsonUserSettingsStore(string filePath)
    {
        _filePath = filePath;
    }

    public static string GetDefaultFilePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "user-settings.json");

    public BrowserSettingsDto? Current => _current;

    public string FilePath => _filePath;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _current = null;

        if (!File.Exists(_filePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            _current = await JsonSerializer.DeserializeAsync<BrowserSettingsDto>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _current = null;
        }
    }

    public async Task SaveAsync(BrowserSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);

        File.Move(tempPath, _filePath, overwrite: true);
        _current = settings;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _current = null;
        if (File.Exists(_filePath))
            File.Delete(_filePath);
        return Task.CompletedTask;
    }
}

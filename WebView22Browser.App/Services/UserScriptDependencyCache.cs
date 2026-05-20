using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public sealed class UserScriptDependencyCache : IUserScriptDependencyCache
{
    public const int MaxFileBytes = 5 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public UserScriptDependencyCache(HttpClient httpClient, BrowserOptions options)
    {
        _httpClient = httpClient;
        _cacheDirectory = options.GetDefaultScriptDepsDirectoryPath();
    }

    public UserScriptDependencyCache(HttpClient httpClient, string cacheDirectory)
    {
        _httpClient = httpClient;
        _cacheDirectory = cacheDirectory;
    }

    public async Task<DependencyFetchResult> GetOrFetchAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeUrl(url, out var normalized, out var validationError))
            return new DependencyFetchResult { Success = false, Error = validationError };

        var cachePath = GetCacheFilePath(normalized);
        if (File.Exists(cachePath))
        {
            try
            {
                var cached = await File.ReadAllTextAsync(cachePath, cancellationToken);
                Debug.WriteLine($"[userscript-deps] cache hit {normalized} ({cached.Length} chars)");
                return new DependencyFetchResult { Success = true, Content = cached };
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"[userscript-deps] cache read failed {normalized}: {ex.Message}");
            }
        }

        try
        {
            using var response = await _httpClient.GetAsync(normalized, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new DependencyFetchResult
                {
                    Success = false,
                    Error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            var bytes = await ReadBodyWithLimitAsync(response.Content, cancellationToken);
            var text = Encoding.UTF8.GetString(bytes);
            Directory.CreateDirectory(_cacheDirectory);
            await File.WriteAllTextAsync(cachePath, text, cancellationToken);
            Debug.WriteLine($"[userscript-deps] fetched {normalized} ({bytes.Length} bytes)");
            return new DependencyFetchResult { Success = true, Content = text };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            return new DependencyFetchResult { Success = false, Error = ex.Message };
        }
    }

    internal static bool TryNormalizeUrl(string url, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            error = "URL 为空。";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            error = $"仅支持 http/https URL：{url}";
            return false;
        }

        normalized = uri.ToString();
        return true;
    }

    private string GetCacheFilePath(string normalizedUrl)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedUrl)));
        return Path.Combine(_cacheDirectory, hash + ".js");
    }

    private static async Task<byte[]> ReadBodyWithLimitAsync(HttpContent content, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + read > MaxFileBytes)
                throw new IOException($"依赖文件超过 {MaxFileBytes} 字节上限。");

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }
}

using System.IO;
using System.Net.Http;
using System.Text;
using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public sealed class GmXhrService : IGmXhrService
{
    public const int MaxResponseBytes = 10 * 1024 * 1024;

    private readonly HttpClient _httpClient;

    public GmXhrService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<GmXhrResult> ExecuteAsync(
        GmXhrRequest request,
        IReadOnlyList<GmCookie> cookies,
        CancellationToken cancellationToken = default)
    {
        using var httpRequest = new HttpRequestMessage(
            new HttpMethod(request.Method),
            request.Url);

        if (!string.IsNullOrEmpty(request.Data)
            && request.Method is not ("GET" or "HEAD"))
        {
            httpRequest.Content = new StringContent(request.Data, Encoding.UTF8);
        }

        ApplyHeaders(httpRequest, request.Headers);
        ApplyCookies(httpRequest, cookies);

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        var bodyBytes = await ReadBodyWithLimitAsync(response.Content, cancellationToken);
        var responseType = request.ResponseType.ToLowerInvariant();
        string? responseText = null;
        string? responseBase64 = null;

        if (responseType is "arraybuffer" or "blob")
            responseBase64 = Convert.ToBase64String(bodyBytes);
        else
            responseText = Encoding.UTF8.GetString(bodyBytes);

        return new GmXhrResult
        {
            Status = (int)response.StatusCode,
            StatusText = response.ReasonPhrase ?? response.StatusCode.ToString(),
            ResponseHeaders = response.Headers
                .Concat(response.Content.Headers)
                .GroupBy(h => h.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.SelectMany(x => x.Value)), StringComparer.OrdinalIgnoreCase),
            ResponseText = responseText,
            ResponseBase64 = responseBase64,
            FinalUrl = response.RequestMessage?.RequestUri?.ToString() ?? request.Url
        };
    }

    private static void ApplyHeaders(HttpRequestMessage httpRequest, IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null)
            return;

        foreach (var (key, value) in headers)
        {
            if (string.IsNullOrWhiteSpace(key))
                continue;

            if (key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!httpRequest.Headers.TryAddWithoutValidation(key, value))
                httpRequest.Content?.Headers.TryAddWithoutValidation(key, value);
        }
    }

    private static void ApplyCookies(HttpRequestMessage httpRequest, IReadOnlyList<GmCookie> cookies)
    {
        if (cookies.Count == 0)
            return;

        var cookieHeader = string.Join("; ", cookies.Select(c => $"{c.Name}={c.Value}"));
        httpRequest.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
    }

    private static async Task<byte[]> ReadBodyWithLimitAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var memory = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + read > MaxResponseBytes)
                throw new InvalidOperationException($"Response exceeds {MaxResponseBytes} bytes.");

            memory.Write(buffer, 0, read);
        }

        return memory.ToArray();
    }
}

using System.Text.Json;

namespace WebView22Browser.Tests;

/// <summary>
/// Ports GM_xmlhttpRequest JSON responseType handling in bootstrap onload.
/// </summary>
internal static class UserScriptBootstrapXhrJsonParity
{
    public sealed record XhrResponse(
        int Status,
        string? ResponseText,
        object? Response,
        string? FinalUrl = null);

    public static (bool Success, XhrResponse? Response, string? Error) ApplyJsonResponseType(
        string? responseType,
        XhrResponse response)
    {
        if (!string.Equals(responseType, "json", StringComparison.OrdinalIgnoreCase))
            return (true, response, null);

        var raw = response.Response ?? response.ResponseText;
        if (raw is not string rawText)
            return (true, response, null);

        try
        {
            using var doc = JsonDocument.Parse(rawText);
            var parsed = doc.RootElement.Clone();
            return (true, response with { Response = parsed, ResponseText = rawText }, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }
}

using System.Text.Json;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class SecurityStateDevToolsParser
{
    public static AddressBarSecurityState ParseVisibleSecurityStateChanged(string? parameterJson)
    {
        if (string.IsNullOrWhiteSpace(parameterJson))
            return AddressBarSecurityState.Unknown;

        try
        {
            using var doc = JsonDocument.Parse(parameterJson);
            if (!doc.RootElement.TryGetProperty("visibleSecurityState", out var visible))
                return AddressBarSecurityState.Unknown;

            if (!visible.TryGetProperty("securityState", out var stateElement))
                return AddressBarSecurityState.Unknown;

            return MapDevToolsState(stateElement.GetString());
        }
        catch (JsonException)
        {
            return AddressBarSecurityState.Unknown;
        }
    }

    public static AddressBarSecurityState FromUriScheme(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return AddressBarSecurityState.Unknown;

        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            return AddressBarSecurityState.Unknown;

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            return AddressBarSecurityState.Secure;

        if (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            return AddressBarSecurityState.Insecure;

        return AddressBarSecurityState.Unknown;
    }

    public static bool UsesHttpSecurityScheme(string? uriString)
    {
        if (string.IsNullOrWhiteSpace(uriString))
            return false;

        if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
               || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
    }

    internal static AddressBarSecurityState MapDevToolsState(string? securityState)
    {
        return securityState?.ToLowerInvariant() switch
        {
            "secure" => AddressBarSecurityState.Secure,
            "neutral" or "info" => AddressBarSecurityState.Neutral,
            "insecure" => AddressBarSecurityState.Insecure,
            "insecure-broken" or "dangerous" => AddressBarSecurityState.Dangerous,
            _ => AddressBarSecurityState.Unknown
        };
    }
}

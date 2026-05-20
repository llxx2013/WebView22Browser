using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WebView22Browser.Core.Services;

public sealed class UserScriptMessageValidationResult
{
    public required string Type { get; init; }

    public JsonElement Payload { get; init; }

    public Guid? ScriptId { get; init; }

    public bool RequiresNonce { get; init; }
}

public static class UserScriptMessageValidator
{
    public const int MaxMessageBytes = 8 * 1024;

    private static readonly HashSet<string> LegacyTypes = new(StringComparer.Ordinal)
    {
        "notify",
        "log"
    };

    public static bool IsAllowedSource(string? sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return false;

        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
            return false;

        // Only http(s) and file pages may post user-script messages. Any other scheme
        // (about:, edge:, chrome:, data:, javascript:, ...) is rejected by the allow-list.
        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https" or "file"))
            return false;

        if (scheme == "file")
            return true;

        return !string.IsNullOrEmpty(uri.Host);
    }

    public static bool TryValidate(
        string? json,
        string? sourceUrl,
        IReadOnlyDictionary<Guid, string>? noncesByScriptId,
        out UserScriptMessageValidationResult? result)
    {
        result = null;

        if (!IsAllowedSource(sourceUrl))
            return false;

        if (string.IsNullOrEmpty(json) || json.Length > MaxMessageBytes)
            return false;

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;

            if (!root.TryGetProperty("type", out var typeProp))
                return false;

            var type = typeProp.GetString();
            if (string.IsNullOrEmpty(type))
                return false;

            root.TryGetProperty("payload", out var payload);

            var hasScriptId = root.TryGetProperty("scriptId", out var scriptIdProp)
                              && scriptIdProp.ValueKind == JsonValueKind.String
                              && Guid.TryParse(scriptIdProp.GetString(), out _);

            var hasNonce = root.TryGetProperty("nonce", out var nonceProp)
                           && nonceProp.ValueKind == JsonValueKind.String
                           && !string.IsNullOrEmpty(nonceProp.GetString());

            if (LegacyTypes.Contains(type))
            {
                result = new UserScriptMessageValidationResult
                {
                    Type = type,
                    Payload = payload.ValueKind == JsonValueKind.Undefined
                        ? default
                        : payload.Clone(),
                    ScriptId = hasScriptId && Guid.TryParse(scriptIdProp.GetString(), out var legacyId)
                        ? legacyId
                        : null,
                    RequiresNonce = false
                };
                return true;
            }

            if (!hasScriptId || !hasNonce)
                return false;

            if (!Guid.TryParse(scriptIdProp.GetString(), out var scriptId))
                return false;

            var nonce = nonceProp.GetString()!;
            if (noncesByScriptId == null
                || !noncesByScriptId.TryGetValue(scriptId, out var expectedNonce)
                || !NoncesMatch(expectedNonce, nonce))
                return false;

            result = new UserScriptMessageValidationResult
            {
                Type = type,
                Payload = payload.ValueKind == JsonValueKind.Undefined
                    ? default
                    : payload.Clone(),
                ScriptId = scriptId,
                RequiresNonce = true
            };
            return true;
        }
    }

    public static bool NoncesMatch(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return expectedBytes.Length == providedBytes.Length
               && CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
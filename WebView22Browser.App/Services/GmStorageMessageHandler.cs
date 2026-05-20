using System.Text.Json;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class GmStorageMessageHandler
{
    private readonly IGmStorageStore _store;

    public GmStorageMessageHandler(IGmStorageStore store)
    {
        _store = store;
    }

    public async Task<bool> HandleAsync(
        UserScriptMessageValidationResult validation,
        CancellationToken cancellationToken = default)
    {
        if (!validation.ScriptId.HasValue)
            return false;

        return validation.Type switch
        {
            "gm.setValue" => await HandleSetValueAsync(validation, cancellationToken),
            "gm.deleteValue" => await HandleDeleteValueAsync(validation, cancellationToken),
            _ => false
        };
    }

    private async Task<bool> HandleSetValueAsync(
        UserScriptMessageValidationResult validation,
        CancellationToken cancellationToken)
    {
        if (validation.Payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!validation.Payload.TryGetProperty("key", out var keyProp) || keyProp.ValueKind != JsonValueKind.String)
            return false;

        if (!validation.Payload.TryGetProperty("value", out var valueProp))
            return false;

        var key = keyProp.GetString();
        if (string.IsNullOrEmpty(key))
            return false;

        await _store.SetValueAsync(validation.ScriptId!.Value, key, valueProp.Clone(), cancellationToken);
        return true;
    }

    private async Task<bool> HandleDeleteValueAsync(
        UserScriptMessageValidationResult validation,
        CancellationToken cancellationToken)
    {
        if (validation.Payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!validation.Payload.TryGetProperty("key", out var keyProp) || keyProp.ValueKind != JsonValueKind.String)
            return false;

        var key = keyProp.GetString();
        if (string.IsNullOrEmpty(key))
            return false;

        await _store.DeleteValueAsync(validation.ScriptId!.Value, key, cancellationToken);
        return true;
    }
}

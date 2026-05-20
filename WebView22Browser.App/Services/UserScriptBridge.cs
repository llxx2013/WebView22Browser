using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class UserScriptBridge
{
    private static readonly HashSet<string> DispatchableTypes = new(StringComparer.Ordinal)
    {
        "notify",
        "log",
        "gm.setValue",
        "gm.deleteValue",
        "gm.xhrRequest",
        "gm.xhrAbort",
        "gm.openInTab",
        "gm.setClipboard",
        "gm.notification",
        "gm.log",
        "gm.registerMenuCommand",
        "gm.unregisterMenuCommand"
    };

    private readonly IDialogService _dialogService;
    private readonly GmStorageMessageHandler _gmStorageHandler;
    private readonly GmXhrMessageHandler _gmXhrHandler;
    private readonly IGmTabService _gmTabService;
    private readonly IGmClipboardService _gmClipboardService;
    private readonly GmMenuCommandRegistry _menuCommandRegistry;
    private readonly object _handlersSync = new();
    private readonly Dictionary<CoreWebView2, EventHandler<CoreWebView2WebMessageReceivedEventArgs>> _handlers = new();
    private readonly ConcurrentDictionary<CoreWebView2, IReadOnlyDictionary<Guid, string>> _nonces = new();

    public UserScriptBridge(
        IDialogService dialogService,
        IGmStorageStore gmStorageStore,
        GmXhrMessageHandler gmXhrHandler,
        IGmTabService gmTabService,
        IGmClipboardService gmClipboardService,
        GmMenuCommandRegistry menuCommandRegistry)
    {
        _dialogService = dialogService;
        _gmStorageHandler = new GmStorageMessageHandler(gmStorageStore);
        _gmXhrHandler = gmXhrHandler;
        _gmTabService = gmTabService;
        _gmClipboardService = gmClipboardService;
        _menuCommandRegistry = menuCommandRegistry;
    }

    public void RegisterNonces(CoreWebView2 core, IReadOnlyDictionary<Guid, string> noncesByScriptId)
    {
        _nonces[core] = noncesByScriptId;
    }

    public void ClearNonces(CoreWebView2 core) => _nonces.TryRemove(core, out _);

    public void ClearMenuCommands(CoreWebView2 core) => _menuCommandRegistry.Clear(core);

    public GmMenuCommandRegistry MenuCommands => _menuCommandRegistry;

    public void Wire(CoreWebView2 core)
    {
        lock (_handlersSync)
        {
            if (_handlers.ContainsKey(core))
                return;

            core.Settings.IsWebMessageEnabled = true;
            EventHandler<CoreWebView2WebMessageReceivedEventArgs> handler = (_, e) => OnWebMessageReceived(core, e);
            _handlers[core] = handler;
            core.WebMessageReceived += handler;
        }
    }

    public void Unwire(CoreWebView2 core)
    {
        EventHandler<CoreWebView2WebMessageReceivedEventArgs>? handler;
        lock (_handlersSync)
        {
            if (!_handlers.TryGetValue(core, out handler))
                return;

            _handlers.Remove(core);
        }

        core.WebMessageReceived -= handler;
        ClearNonces(core);
        _menuCommandRegistry.Clear(core);
    }

    private void OnWebMessageReceived(CoreWebView2 core, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string json;
        try
        {
            json = e.TryGetWebMessageAsString();
        }
        catch
        {
            return;
        }

        _nonces.TryGetValue(core, out var noncesByScriptId);

        if (!UserScriptMessageValidator.TryValidate(json, e.Source, noncesByScriptId, out var validation)
            || validation == null)
        {
            Debug.WriteLine($"[userscript] rejected message from {e.Source}");
            return;
        }

        if (!DispatchableTypes.Contains(validation.Type))
        {
            Debug.WriteLine($"[userscript] undispatchable type '{validation.Type}' from {e.Source}");
            return;
        }

        switch (validation.Type)
        {
            case "notify":
            case "gm.notification":
                HandleNotify(validation.Payload);
                break;
            case "log":
            case "gm.log":
                HandleLog(validation.Payload);
                break;
            case "gm.setValue":
            case "gm.deleteValue":
                _ = PersistGmStorageAsync(validation);
                break;
            case "gm.xhrRequest":
            case "gm.xhrAbort":
                _ = HandleGmXhrAsync(validation, core);
                break;
            case "gm.openInTab":
                HandleOpenInTab(validation.Payload);
                break;
            case "gm.setClipboard":
                HandleSetClipboard(validation.Payload);
                break;
            case "gm.registerMenuCommand":
                HandleRegisterMenuCommand(core, validation);
                break;
            case "gm.unregisterMenuCommand":
                HandleUnregisterMenuCommand(core, validation);
                break;
        }
    }

    private void HandleRegisterMenuCommand(CoreWebView2 core, UserScriptMessageValidationResult validation)
    {
        if (!validation.ScriptId.HasValue)
            return;

        if (!TryParseMenuCommandPayload(validation.Payload, out var commandId, out var caption))
            return;

        string? accessKey = null;
        if (validation.Payload.ValueKind == JsonValueKind.Object
            && validation.Payload.TryGetProperty("accessKey", out var accessKeyProp)
            && accessKeyProp.ValueKind == JsonValueKind.String)
        {
            var parsed = accessKeyProp.GetString();
            if (!string.IsNullOrEmpty(parsed))
                accessKey = parsed;
        }

        _menuCommandRegistry.Register(
            core,
            new GmMenuCommandDescriptor(commandId, validation.ScriptId.Value, caption, accessKey));
    }

    private void HandleUnregisterMenuCommand(CoreWebView2 core, UserScriptMessageValidationResult validation)
    {
        if (!validation.ScriptId.HasValue)
            return;

        if (!TryParseMenuCommandId(validation.Payload, out var commandId))
            return;

        _menuCommandRegistry.Unregister(core, validation.ScriptId.Value, commandId);
    }

    private static bool TryParseMenuCommandPayload(JsonElement payload, out int commandId, out string caption)
    {
        commandId = 0;
        caption = string.Empty;

        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!payload.TryGetProperty("commandId", out var idProp) || idProp.ValueKind != JsonValueKind.Number)
            return false;

        commandId = idProp.GetInt32();

        if (!payload.TryGetProperty("caption", out var captionProp) || captionProp.ValueKind != JsonValueKind.String)
            return false;

        caption = captionProp.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(caption);
    }

    private static bool TryParseMenuCommandId(JsonElement payload, out int commandId)
    {
        commandId = 0;

        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!payload.TryGetProperty("commandId", out var idProp) || idProp.ValueKind != JsonValueKind.Number)
            return false;

        commandId = idProp.GetInt32();
        return true;
    }

    private async Task HandleGmXhrAsync(UserScriptMessageValidationResult validation, CoreWebView2 core)
    {
        try
        {
            await _gmXhrHandler.HandleAsync(validation, core);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[userscript] GM XHR failed: {ex}");
        }
    }

    private async Task PersistGmStorageAsync(UserScriptMessageValidationResult validation)
    {
        try
        {
            await _gmStorageHandler.HandleAsync(validation);
        }
        catch (GmStorageQuotaExceededException ex)
        {
            Debug.WriteLine($"[userscript] GM storage quota exceeded: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[userscript] GM storage persist failed: {ex}");
        }
    }

    private void HandleNotify(JsonElement payload)
    {
        var message = GetMessageText(payload, "message") ?? GetMessageText(payload, "text");
        if (string.IsNullOrWhiteSpace(message))
            return;

        var title = GetMessageText(payload, "title") ?? "用户脚本";
        RunOnUiThread(() => _dialogService.ShowInfo(message, title));
    }

    private static void HandleLog(JsonElement payload)
    {
        var message = GetMessageText(payload, "message");
        if (message != null)
            Debug.WriteLine($"[userscript] {message}");
    }

    private void HandleOpenInTab(JsonElement payload)
    {
        if (!payload.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String)
            return;

        var url = urlProp.GetString();
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (payload.TryGetProperty("active", out var activeProp)
            && activeProp.ValueKind == JsonValueKind.False)
        {
            return;
        }

        RunOnUiThread(() => _gmTabService.OpenInNewTab(url));
    }

    private void HandleSetClipboard(JsonElement payload)
    {
        if (!payload.TryGetProperty("data", out var dataProp) || dataProp.ValueKind != JsonValueKind.String)
            return;

        var data = dataProp.GetString();
        if (data == null)
            return;

        var clipType = "text";
        if (payload.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
        {
            var parsed = typeProp.GetString();
            if (!string.IsNullOrEmpty(parsed))
                clipType = parsed;
        }

        if (!string.Equals(clipType, "text", StringComparison.OrdinalIgnoreCase))
            return;

        RunOnUiThread(() => _gmClipboardService.SetText(data));
    }

    private static string? GetMessageText(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return null;

        if (!payload.TryGetProperty(propertyName, out var prop) || prop.ValueKind != JsonValueKind.String)
            return null;

        return prop.GetString();
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action, DispatcherPriority.Normal);
    }
}
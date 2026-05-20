using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class GmXhrMessageHandler
{
    private readonly IGmXhrService _xhrService;
    private readonly IUserScriptStore _scriptStore;
    private readonly IGmXhrReplyChannel? _replyChannel;
    private readonly Func<CoreWebView2, string, CancellationToken, Task<IReadOnlyList<GmCookie>>>? _getCookies;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _inflight = new();

    public GmXhrMessageHandler(
        IGmXhrService xhrService,
        IUserScriptStore scriptStore,
        IGmXhrReplyChannel? replyChannel = null,
        Func<CoreWebView2, string, CancellationToken, Task<IReadOnlyList<GmCookie>>>? getCookies = null)
    {
        _xhrService = xhrService;
        _scriptStore = scriptStore;
        _replyChannel = replyChannel;
        _getCookies = getCookies;
    }

    public Task HandleAsync(
        UserScriptMessageValidationResult validation,
        CoreWebView2 core,
        CancellationToken cancellationToken = default) =>
        validation.Type switch
        {
            "gm.xhrRequest" => HandleRequestAsync(validation, core, cancellationToken),
            "gm.xhrAbort" => HandleAbortAsync(validation, core, cancellationToken),
            _ => Task.CompletedTask
        };

    private async Task HandleRequestAsync(
        UserScriptMessageValidationResult validation,
        CoreWebView2 core,
        CancellationToken cancellationToken)
    {
        if (!validation.ScriptId.HasValue)
            return;

        var scriptId = validation.ScriptId.Value;
        if (!TryParseRequest(validation.Payload, out var requestId, out var xhrRequest))
        {
            await ReplyErrorAsync(core, scriptId, requestId ?? string.Empty, "Invalid GM_xmlhttpRequest payload.");
            return;
        }

        var script = _scriptStore.FindById(scriptId);
        if (script == null)
        {
            await ReplyErrorAsync(core, scriptId, requestId!, "Script not found.");
            return;
        }

        if (!UserScriptConnectMatcher.IsAllowed(xhrRequest!.Url, script.ConnectPatterns, script.MatchPatterns))
        {
            await ReplyErrorAsync(core, scriptId, requestId!, "@connect denied.");
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (xhrRequest.TimeoutMs > 0)
            linkedCts.CancelAfter(xhrRequest.TimeoutMs);

        _inflight[requestId!] = linkedCts;

        try
        {
            var cookies = _getCookies != null
                ? await _getCookies(core, xhrRequest.Url, linkedCts.Token)
                : await GetCookiesFromWebViewAsync(core, xhrRequest.Url, linkedCts.Token);

            var result = await _xhrService.ExecuteAsync(xhrRequest, cookies, linkedCts.Token);
            var responseHeaders = string.Join(
                "\r\n",
                result.ResponseHeaders.Select(pair => $"{pair.Key}: {pair.Value}"));

            var responseType = xhrRequest.ResponseType.ToLowerInvariant();
            var body = responseType is "arraybuffer" or "blob"
                ? result.ResponseBase64
                : result.ResponseText;

            await ReplyAsync(core, new GmXhrResponseEnvelope
            {
                RequestId = requestId!,
                ScriptId = scriptId.ToString(),
                Kind = "load",
                Response = new
                {
                    status = result.Status,
                    statusText = result.StatusText,
                    responseText = result.ResponseText ?? body,
                    response = body,
                    finalUrl = result.FinalUrl,
                    responseHeaders
                }
            }, linkedCts.Token);
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            if (_inflight.TryRemove(requestId!, out var removedCts))
            {
                removedCts.Dispose();
                var kind = xhrRequest.TimeoutMs > 0 ? "timeout" : "abort";
                await ReplyAsync(core, new GmXhrResponseEnvelope
                {
                    RequestId = requestId!,
                    ScriptId = scriptId.ToString(),
                    Kind = kind
                }, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            await ReplyErrorAsync(core, scriptId, requestId!, ex.Message);
        }
        finally
        {
            if (_inflight.TryRemove(requestId!, out var remainingCts))
                remainingCts.Dispose();
        }
    }

    private async Task HandleAbortAsync(
        UserScriptMessageValidationResult validation,
        CoreWebView2 core,
        CancellationToken cancellationToken)
    {
        if (!validation.ScriptId.HasValue)
            return;

        if (!validation.Payload.TryGetProperty("requestId", out var idProp)
            || idProp.ValueKind != JsonValueKind.String)
            return;

        var requestId = idProp.GetString();
        if (string.IsNullOrEmpty(requestId))
            return;

        if (_inflight.TryRemove(requestId, out var cts))
        {
            await cts.CancelAsync();
            cts.Dispose();
        }

        await ReplyAsync(core, new GmXhrResponseEnvelope
        {
            RequestId = requestId,
            ScriptId = validation.ScriptId.Value.ToString(),
            Kind = "abort"
        }, cancellationToken);
    }

    private static bool TryParseRequest(
        JsonElement payload,
        out string? requestId,
        out GmXhrRequest? request)
    {
        requestId = null;
        request = null;

        if (payload.ValueKind != JsonValueKind.Object)
            return false;

        if (!payload.TryGetProperty("requestId", out var idProp) || idProp.ValueKind != JsonValueKind.String)
            return false;

        requestId = idProp.GetString();
        if (string.IsNullOrEmpty(requestId))
            return false;

        if (!payload.TryGetProperty("url", out var urlProp) || urlProp.ValueKind != JsonValueKind.String)
            return false;

        var url = urlProp.GetString();
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var method = "GET";
        if (payload.TryGetProperty("method", out var methodProp) && methodProp.ValueKind == JsonValueKind.String)
        {
            var parsedMethod = methodProp.GetString();
            if (!string.IsNullOrWhiteSpace(parsedMethod))
                method = parsedMethod;
        }

        string? data = null;
        if (payload.TryGetProperty("data", out var dataProp) && dataProp.ValueKind == JsonValueKind.String)
            data = dataProp.GetString();

        var timeoutMs = 0;
        if (payload.TryGetProperty("timeoutMs", out var timeoutProp) && timeoutProp.TryGetInt32(out var parsedTimeout))
            timeoutMs = parsedTimeout;

        var responseType = "text";
        if (payload.TryGetProperty("responseType", out var typeProp) && typeProp.ValueKind == JsonValueKind.String)
        {
            var parsedType = typeProp.GetString();
            if (!string.IsNullOrWhiteSpace(parsedType))
                responseType = parsedType;
        }

        IReadOnlyDictionary<string, string>? headers = null;
        if (payload.TryGetProperty("headers", out var headersProp)
            && headersProp.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in headersProp.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (value != null)
                        dict[property.Name] = value;
                }
            }

            headers = dict;
        }

        request = new GmXhrRequest
        {
            RequestId = requestId,
            Method = method.ToUpperInvariant(),
            Url = url,
            Headers = headers,
            Data = data,
            TimeoutMs = timeoutMs,
            ResponseType = responseType
        };
        return true;
    }

    private static async Task<IReadOnlyList<GmCookie>> GetCookiesFromWebViewAsync(
        CoreWebView2 core,
        string url,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var cookies = await core.CookieManager.GetCookiesAsync(url);
        return cookies
            .Select(c => new GmCookie(c.Name, c.Value, c.Domain, c.Path))
            .ToList();
    }

    private async Task ReplyErrorAsync(
        CoreWebView2 core,
        Guid scriptId,
        string requestId,
        string error)
    {
        if (string.IsNullOrEmpty(requestId))
            return;

        await ReplyAsync(core, new GmXhrResponseEnvelope
        {
            RequestId = requestId,
            ScriptId = scriptId.ToString(),
            Kind = "error",
            Error = error
        }, CancellationToken.None);
    }

    private Task ReplyAsync(
        CoreWebView2 core,
        GmXhrResponseEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (_replyChannel != null)
            return _replyChannel.ReplyAsync(envelope, cancellationToken);

        return Wv2GmXhrReplyChannel.ReplyAsync(core, envelope, cancellationToken);
    }
}
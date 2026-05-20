using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public static class Wv2GmXhrReplyChannel
{
    // Use the strict default JavaScriptEncoder so characters that are syntactically
    // dangerous when the JSON is embedded in a JavaScript source (U+2028, U+2029,
    // `<`, `>`, `&`, etc.) are emitted as \uXXXX escapes. The payload is concatenated
    // into a `__wv2dispatch(...)` call passed to ExecuteScriptAsync, so we cannot rely on
    // a separate JSON-parsing context like postMessage provides.
    private static readonly JsonSerializerOptions ReplyOptions = new()
    {
        Encoder = JavaScriptEncoder.Default
    };

    public static async Task ReplyAsync(
        CoreWebView2 core,
        GmXhrResponseEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var json = JsonSerializer.Serialize(envelope, ReplyOptions);
        await core.ExecuteScriptAsync($"__wv2dispatch({json})");
    }
}
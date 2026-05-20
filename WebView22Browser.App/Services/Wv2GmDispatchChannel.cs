using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace WebView22Browser.App.Services;

public static class Wv2GmDispatchChannel
{
    private static readonly JsonSerializerOptions DispatchOptions = new()
    {
        Encoder = JavaScriptEncoder.Default
    };

    public static Task DispatchAsync(CoreWebView2 core, object payload)
    {
        var json = JsonSerializer.Serialize(payload, DispatchOptions);
        return core.ExecuteScriptAsync($"__wv2dispatch({json})");
    }
}

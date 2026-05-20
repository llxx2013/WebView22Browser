using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public interface IGmXhrService
{
    Task<GmXhrResult> ExecuteAsync(
        GmXhrRequest request,
        IReadOnlyList<GmCookie> cookies,
        CancellationToken cancellationToken = default);
}
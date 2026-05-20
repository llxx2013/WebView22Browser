using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public interface IGmXhrReplyChannel
{
    Task ReplyAsync(GmXhrResponseEnvelope envelope, CancellationToken cancellationToken = default);
}

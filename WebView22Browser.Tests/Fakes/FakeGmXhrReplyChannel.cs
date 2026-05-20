using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeGmXhrReplyChannel : IGmXhrReplyChannel
{
    public List<GmXhrResponseEnvelope> Replies { get; } = [];

    public Task ReplyAsync(GmXhrResponseEnvelope envelope, CancellationToken cancellationToken = default)
    {
        Replies.Add(envelope);
        return Task.CompletedTask;
    }
}

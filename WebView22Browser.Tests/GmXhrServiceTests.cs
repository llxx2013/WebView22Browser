using System.Net;
using System.Text;

using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Tests;

public class GmXhrServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsTextResponse()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("hello", Encoding.UTF8, "text/plain")
        });
        var service = new GmXhrService(new HttpClient(handler));

        var result = await service.ExecuteAsync(
            new GmXhrRequest { RequestId = "r1", Url = "https://example.com/" },
            [],
            CancellationToken.None);

        Assert.Equal(200, result.Status);
        Assert.Equal("hello", result.ResponseText);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsErrorStatus()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.MethodNotAllowed));
        var service = new GmXhrService(new HttpClient(handler));

        var result = await service.ExecuteAsync(
            new GmXhrRequest { RequestId = "r1", Url = "https://example.com/" },
            [],
            CancellationToken.None);

        Assert.Equal(405, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_HonorsCancellation()
    {
        var handler = new StubHttpMessageHandler(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var service = new GmXhrService(new HttpClient(handler));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ExecuteAsync(
                new GmXhrRequest { RequestId = "r1", Url = "https://example.com/" },
                [],
                cts.Token));
    }

    [Fact]
    public async Task ExecuteAsync_OversizedResponse_Throws()
    {
        var oversized = new byte[GmXhrService.MaxResponseBytes + 1];
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(oversized)
        });
        var service = new GmXhrService(new HttpClient(handler));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ExecuteAsync(
                new GmXhrRequest { RequestId = "r1", Url = "https://example.com/" },
                [],
                CancellationToken.None));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            : this((request, _) => Task.FromResult(handler(request)))
        {
        }

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
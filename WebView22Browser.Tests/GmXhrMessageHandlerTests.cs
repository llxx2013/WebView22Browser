using System.Text.Json;

using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class GmXhrMessageHandlerTests
{
    private static readonly Guid ScriptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task HandleAsync_MissingUrl_RepliesError()
    {
        var (handler, replies, _) = CreateHandler(new FakeGmXhrService());

        using var payloadDoc = JsonDocument.Parse("""{"requestId":"req-1"}""");
        var validation = CreateValidation("gm.xhrRequest", payloadDoc.RootElement);

        await handler.HandleAsync(validation, null!);

        Assert.Contains(replies.Replies, r => r.Kind == "error" && r.RequestId == "req-1");
    }

    [Fact]
    public async Task HandleAsync_ConnectDenied_RepliesError()
    {
        var store = CreateStore(connect: ["example.com"], match: ["https://other.com/*"]);
        var xhr = new FakeGmXhrService();
        var (handler, replies, _) = CreateHandler(xhr, store);

        using var payloadDoc = JsonDocument.Parse(
            """{"requestId":"req-2","url":"https://denied.org/","method":"GET"}""");
        var validation = CreateValidation("gm.xhrRequest", payloadDoc.RootElement);

        await handler.HandleAsync(validation, null!);

        Assert.Contains(replies.Replies, r => r.Kind == "error" && r.Error!.Contains("@connect"));
        Assert.Empty(xhr.Executed);
    }

    [Fact]
    public async Task HandleAsync_Success_RepliesLoad()
    {
        var store = CreateStore(connect: ["*"], match: ["https://example.com/*"]);
        var xhr = new FakeGmXhrService
        {
            Result = new GmXhrResult
            {
                Status = 200,
                StatusText = "OK",
                ResponseText = "body",
                FinalUrl = "https://example.com/"
            }
        };
        var (handler, replies, _) = CreateHandler(xhr, store);

        using var payloadDoc = JsonDocument.Parse(
            """{"requestId":"req-3","url":"https://example.com/","method":"GET"}""");
        var validation = CreateValidation("gm.xhrRequest", payloadDoc.RootElement);

        await handler.HandleAsync(validation, null!);

        Assert.Single(xhr.Executed);
        Assert.Contains(replies.Replies, r => r.Kind == "load" && r.RequestId == "req-3");
    }

    [Fact]
    public async Task HandleAsync_Abort_CancelsInflightRequest()
    {
        var store = CreateStore(connect: ["*"], match: ["https://example.com/*"]);
        var xhr = new FakeGmXhrService { Delay = TimeSpan.FromSeconds(5) };
        var (handler, replies, _) = CreateHandler(xhr, store);

        using var requestDoc = JsonDocument.Parse(
            """{"requestId":"req-4","url":"https://example.com/","method":"GET","timeoutMs":30000}""");
        var requestValidation = CreateValidation("gm.xhrRequest", requestDoc.RootElement);
        var requestTask = handler.HandleAsync(requestValidation, null!);

        using var abortDoc = JsonDocument.Parse("""{"requestId":"req-4"}""");
        var abortValidation = CreateValidation("gm.xhrAbort", abortDoc.RootElement);
        await handler.HandleAsync(abortValidation, null!);

        await requestTask;

        Assert.Contains(replies.Replies, r => r.Kind == "abort" && r.RequestId == "req-4");
    }

    private static (GmXhrMessageHandler Handler, FakeGmXhrReplyChannel Replies, JsonUserScriptStore Store) CreateHandler(
        FakeGmXhrService xhrService,
        JsonUserScriptStore? store = null)
    {
        store ??= CreateStore(connect: ["*"], match: ["https://example.com/*"]);
        var replies = new FakeGmXhrReplyChannel();
        var handler = new GmXhrMessageHandler(
            xhrService,
            store,
            replies,
            (_, _, _) => Task.FromResult<IReadOnlyList<GmCookie>>([]));

        return (handler, replies, store);
    }

    private static JsonUserScriptStore CreateStore(string[] connect, string[] match)
    {
        var path = Path.Combine(Path.GetTempPath(), $"xhr-handler-{Guid.NewGuid():N}.json");
        var store = new JsonUserScriptStore(path);
        store.Add(new UserScriptEntry
        {
            Id = ScriptId,
            Name = "Test",
            Enabled = true,
            ConnectPatterns = connect,
            MatchPatterns = match,
            Code = "void 0;"
        });
        return store;
    }

    private static UserScriptMessageValidationResult CreateValidation(string type, JsonElement payload) =>
        new()
        {
            Type = type,
            Payload = payload,
            ScriptId = ScriptId,
            RequiresNonce = true
        };

    private sealed class FakeGmXhrService : IGmXhrService
    {
        public List<GmXhrRequest> Executed { get; } = [];
        public GmXhrResult Result { get; set; } = new() { Status = 200, FinalUrl = "https://example.com/" };
        public TimeSpan Delay { get; set; }

        public async Task<GmXhrResult> ExecuteAsync(
            GmXhrRequest request,
            IReadOnlyList<GmCookie> cookies,
            CancellationToken cancellationToken)
        {
            Executed.Add(request);
            if (Delay > TimeSpan.Zero)
                await Task.Delay(Delay, cancellationToken);

            return Result;
        }
    }
}
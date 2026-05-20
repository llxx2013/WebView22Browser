using System.Text.Json;
using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class GmStorageMessageHandlerTests : IDisposable
{
    private readonly string _root;
    private readonly JsonGmStorageStore _store;
    private readonly GmStorageMessageHandler _handler;
    private readonly Guid _scriptId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public GmStorageMessageHandlerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"gm-handler-{Guid.NewGuid():N}");
        _store = new JsonGmStorageStore(_root, new GmStorageQuota());
        _handler = new GmStorageMessageHandler(_store);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ignored
        }
    }

    [Fact]
    public async Task HandleAsync_SetValue_Persists()
    {
        using var payloadDoc = JsonDocument.Parse("""{"key":"foo","value":"bar"}""");
        var validation = new UserScriptMessageValidationResult
        {
            Type = "gm.setValue",
            Payload = payloadDoc.RootElement,
            ScriptId = _scriptId,
            RequiresNonce = true
        };

        var handled = await _handler.HandleAsync(validation);

        Assert.True(handled);
        var loaded = await _store.LoadAsync(_scriptId);
        Assert.Equal("bar", loaded["foo"].GetString());
    }

    [Fact]
    public async Task HandleAsync_DeleteValue_RemovesKey()
    {
        using var setDoc = JsonDocument.Parse("""{"key":"x","value":1}""");
        await _handler.HandleAsync(new UserScriptMessageValidationResult
        {
            Type = "gm.setValue",
            Payload = setDoc.RootElement,
            ScriptId = _scriptId,
            RequiresNonce = true
        });

        using var deleteDoc = JsonDocument.Parse("""{"key":"x"}""");
        var handled = await _handler.HandleAsync(new UserScriptMessageValidationResult
        {
            Type = "gm.deleteValue",
            Payload = deleteDoc.RootElement,
            ScriptId = _scriptId,
            RequiresNonce = true
        });

        Assert.True(handled);
        var loaded = await _store.LoadAsync(_scriptId);
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task HandleAsync_MissingKey_ReturnsFalse()
    {
        using var payloadDoc = JsonDocument.Parse("""{"value":1}""");
        var handled = await _handler.HandleAsync(new UserScriptMessageValidationResult
        {
            Type = "gm.setValue",
            Payload = payloadDoc.RootElement,
            ScriptId = _scriptId,
            RequiresNonce = true
        });

        Assert.False(handled);
    }

    [Fact]
    public async Task HandleAsync_MissingScriptId_ReturnsFalse()
    {
        using var payloadDoc = JsonDocument.Parse("""{"key":"k","value":1}""");
        var handled = await _handler.HandleAsync(new UserScriptMessageValidationResult
        {
            Type = "gm.setValue",
            Payload = payloadDoc.RootElement,
            ScriptId = null,
            RequiresNonce = true
        });

        Assert.False(handled);
    }
}

using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptMessageValidatorTests
{
    private static readonly Guid ScriptId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly IReadOnlyDictionary<Guid, string> Nonces =
        new Dictionary<Guid, string> { [ScriptId] = "expected-nonce" };

    [Theory]
    [InlineData("https://example.com/page")]
    [InlineData("http://localhost:8080/")]
    [InlineData("file:///C:/temp/page.html")]
    public void IsAllowedSource_ValidUrls_ReturnsTrue(string source)
    {
        Assert.True(UserScriptMessageValidator.IsAllowedSource(source));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("about:blank")]
    [InlineData("edge://settings")]
    [InlineData("not-a-url")]
    public void IsAllowedSource_InvalidUrls_ReturnsFalse(string? source)
    {
        Assert.False(UserScriptMessageValidator.IsAllowedSource(source));
    }

    [Fact]
    public void TryValidate_LegacyNotify_WithoutNonce_Succeeds()
    {
        var json = """{"type":"notify","payload":{"message":"hi"}}""";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("notify", result.Type);
        Assert.False(result.RequiresNonce);
    }

    [Fact]
    public void TryValidate_LegacyLog_WithoutNonce_Succeeds()
    {
        var json = """{"type":"log","payload":{"message":"trace"}}""";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            null,
            out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("log", result.Type);
    }

    [Fact]
    public void TryValidate_GmXhrRequest_Succeeds()
    {
        var json =
            $"{{\"type\":\"gm.xhrRequest\",\"scriptId\":\"{ScriptId}\",\"nonce\":\"expected-nonce\",\"payload\":{{\"requestId\":\"r1\",\"url\":\"https://example.com/\",\"method\":\"GET\"}}}}";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("gm.xhrRequest", result.Type);
        Assert.True(result.Payload.TryGetProperty("url", out _));
    }

    [Fact]
    public void TryValidate_GmSetValue_WithPayload_Succeeds()
    {
        var json =
            $"{{\"type\":\"gm.setValue\",\"scriptId\":\"{ScriptId}\",\"nonce\":\"expected-nonce\",\"payload\":{{\"key\":\"foo\",\"value\":\"bar\"}}}}";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("gm.setValue", result.Type);
        Assert.True(result.Payload.TryGetProperty("key", out _));
        Assert.True(result.Payload.TryGetProperty("value", out _));
    }

    [Fact]
    public void TryValidate_GmMessage_WithValidNonce_Succeeds()
    {
        var json =
            $"{{\"type\":\"gm.setValue\",\"scriptId\":\"{ScriptId}\",\"nonce\":\"expected-nonce\",\"payload\":{{\"key\":\"k\"}}}}";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out var result);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("gm.setValue", result.Type);
        Assert.True(result.RequiresNonce);
        Assert.Equal(ScriptId, result.ScriptId);
    }

    [Fact]
    public void TryValidate_GmMessage_WithForgedNonce_Fails()
    {
        var json =
            $"{{\"type\":\"gm.setValue\",\"scriptId\":\"{ScriptId}\",\"nonce\":\"wrong-nonce\",\"payload\":{{}}}}";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryValidate_UnknownType_WithoutAuth_Fails()
    {
        var json = """{"type":"gm.setValue","payload":{}}""";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryValidate_InvalidSource_Fails()
    {
        var json = """{"type":"notify","payload":{"message":"hi"}}""";

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "about:blank",
            Nonces,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryValidate_OversizedJson_Fails()
    {
        var json = new string('a', UserScriptMessageValidator.MaxMessageBytes + 1);

        var ok = UserScriptMessageValidator.TryValidate(
            json,
            "https://example.com/",
            Nonces,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryValidate_InvalidJson_Fails()
    {
        var ok = UserScriptMessageValidator.TryValidate(
            "{not json",
            "https://example.com/",
            Nonces,
            out _);

        Assert.False(ok);
    }

    [Fact]
    public void NoncesMatch_UsesConstantTimeComparison()
    {
        Assert.True(UserScriptMessageValidator.NoncesMatch("abc", "abc"));
        Assert.False(UserScriptMessageValidator.NoncesMatch("abc", "abd"));
    }
}
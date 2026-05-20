using System.Text.Json;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Tests.Fixtures;

namespace WebView22Browser.Tests;

public class TranslateScriptCompatibilityTests
{
    private static UserScriptBootstrapBuilder CreateBuilder() =>
        new(() => "translate-compat-nonce");

    [Fact]
    public void Metadata_Parse_IncludesTranslateGrantsAndDependencies()
    {
        var parsed = TranslateCompatFixture.ParseMetadata();

        Assert.Equal("translate-compat-fixture", parsed.Name);
        Assert.Equal(UserScriptRunAt.DocumentIdle, parsed.RunAt);
        Assert.True(parsed.RunInTopFrameOnly);
        Assert.Equal(3, parsed.RequireUrls.Length);
        Assert.Single(parsed.Resources);
        Assert.Contains("GM_getValue", parsed.Grants);
        Assert.Contains("GM_registerMenuCommand", parsed.Grants);
        Assert.Contains("GM_getResourceText", parsed.Grants);
        Assert.DoesNotContain(parsed.Warnings, w => w.Contains("GM_getResourceText"));
    }

    [Fact]
    public void Bootstrap_IncludesGmPreludeForTranslateGrants()
    {
        var entry = TranslateCompatFixture.CreateEntry();
        var artifact = CreateBuilder().Build([entry]);

        Assert.NotNull(artifact);
        Assert.Contains("'var ' + grantName + ' = GM.' + grantName", artifact.JavaScript);
        Assert.Contains("prelude + rule.code", artifact.JavaScript);
        Assert.Contains("api.GM_getValue", artifact.JavaScript);
        Assert.Contains("api.GM_registerMenuCommand", artifact.JavaScript);
        var prelude = UserScriptBootstrapGmPreludeParity.BuildPrelude(
            TranslateCompatFixture.Grants,
            new HashSet<string>(StringComparer.Ordinal)
            {
                "GM_getValue",
                "GM_setValue",
                "GM_xmlhttpRequest",
                "GM_registerMenuCommand",
                "GM_getResourceText"
            });
        Assert.Contains("var GM_getValue = GM.GM_getValue", prelude);
        Assert.Contains("var GM_registerMenuCommand = GM.GM_registerMenuCommand", prelude);
    }

    [Fact]
    public void Bootstrap_WithResolvedDeps_InjectsRequiresAndResourceText()
    {
        var entry = TranslateCompatFixture.CreateEntry();
        var resolved = new Dictionary<Guid, ResolvedScriptDependencies>
        {
            [entry.Id] = TranslateCompatFixture.CreateResolvedDependencies()
        };

        var artifact = CreateBuilder().Build(
            [entry],
            new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>(),
            resolved);

        Assert.NotNull(artifact);
        Assert.Contains("injectRequireScripts", artifact.JavaScript);
        Assert.Contains("window.__jqueryMock = 1;", artifact.JavaScript);
        Assert.Contains("swalStyle", artifact.JavaScript);
        Assert.Contains(".swal { color: red; }", artifact.JavaScript);
        Assert.Contains("document-idle", artifact.JavaScript);
    }

    [Fact]
    public void Bootstrap_ProvidesJsonXhrHandling_ForTranslateApiShape()
    {
        var artifact = CreateBuilder().Build([TranslateCompatFixture.CreateEntry()]);

        Assert.NotNull(artifact);
        Assert.Contains("details.responseType === 'json'", artifact.JavaScript);
        Assert.Contains("JSON.parse(raw)", artifact.JavaScript);
    }

    [Fact]
    public void FixtureCode_MirrorsTranslateGmCallPatterns()
    {
        var code = TranslateCompatFixture.CreateEntry().Code;

        Assert.Contains("GM_getValue", code);
        Assert.Contains("GM_registerMenuCommand", code);
        Assert.Contains("GM_getResourceText", code);
        Assert.Contains("responseType: 'json'", code);
        Assert.Contains("res.response.code", code);
    }

    [Fact]
    public void XhrJsonParity_TranslateApiShape_ParsesCodeField()
    {
        var (success, response, error) = UserScriptBootstrapXhrJsonParity.ApplyJsonResponseType(
            "json",
            new UserScriptBootstrapXhrJsonParity.XhrResponse(
                200,
                """{"code":200,"data":"hello"}""",
                """{"code":200,"data":"hello"}"""));

        Assert.True(success);
        Assert.Null(error);
        var parsed = Assert.IsType<JsonElement>(response!.Response);
        Assert.Equal(200, parsed.GetProperty("code").GetInt32());
        Assert.Equal("hello", parsed.GetProperty("data").GetString());
    }

    [Fact]
    public void DependencyStatus_WithFullCache_ReportsReady()
    {
        var entry = TranslateCompatFixture.CreateEntry();
        var resolved = TranslateCompatFixture.CreateResolvedDependencies();

        Assert.True(UserScriptDependencyStatus.IsCacheReady(entry, resolved));
        Assert.Equal(
            "Requires 3 · Resources 1 · 缓存就绪",
            UserScriptDependencyStatus.FormatSummary(entry, resolved, isEnabled: true));
    }

    [Fact]
    public void DependencyStatus_WithMissingRequire_ReportsPending()
    {
        var entry = TranslateCompatFixture.CreateEntry();
        var resolved = new ResolvedScriptDependencies
        {
            RequireScripts = ["only-one.js"],
            ResourceTexts = TranslateCompatFixture.CreateResolvedDependencies().ResourceTexts
        };
        resolved.Warnings.Add("无法加载 @require：https://example.test/missing.js");

        Assert.False(UserScriptDependencyStatus.IsCacheReady(entry, resolved));
        Assert.Equal(
            "Requires 3 · Resources 1 · 依赖待刷新",
            UserScriptDependencyStatus.FormatSummary(entry, resolved, isEnabled: true));
    }
}
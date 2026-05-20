using System.Text.Json;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptBootstrapBuilderTests
{
    private static UserScriptBootstrapBuilder CreateBuilder(string nonce = "test-nonce-fixed") =>
        new(() => nonce);

    [Fact]
    public void Build_NoEnabledScripts_ReturnsNull()
    {
        var scripts = new[]
        {
            new UserScriptEntry { Enabled = false, Code = "alert(1);" }
        };

        Assert.Null(CreateBuilder().Build(scripts));
    }

    [Fact]
    public void Build_EscapesUserCodeInJson()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*.example.com/*"],
                Code = "var x = \"quote\\\" and newline\\n\";"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("__wv2browser", artifact.JavaScript);
        Assert.Contains("quote", artifact.JavaScript);
        Assert.DoesNotContain("alert(1)", artifact.JavaScript);
    }

    [Fact]
    public void Build_IncludesMatchHelperAndRules()
    {
        var id = Guid.NewGuid();
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Id = id,
                Enabled = true,
                MatchPatterns = ["https://example.com/*"],
                Code = "document.title = 'x';"
            }
        };

        var artifact = CreateBuilder("nonce-for-rule").Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains(id.ToString(), artifact.JavaScript);
        Assert.Contains("matchUrl", artifact.JavaScript);
        Assert.Contains("nonce-for-rule", artifact.JavaScript);
        Assert.True(artifact.NoncesByScriptId.ContainsKey(id));
        Assert.Equal("nonce-for-rule", artifact.NoncesByScriptId[id]);
    }

    [Fact]
    public void Build_EmbedsRulesViaJsonParseStringLiteral()
    {
        // Defense-in-depth: rules must be embedded as a JSON string literal parsed at
        // runtime via JSON.parse, not as a raw JS object/array literal. This neutralises
        // any future template-literal injection vector via user-controlled script fields.
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Code = "document.title = 'x';"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("var rules = JSON.parse(", artifact.JavaScript);
        Assert.Contains("var noncesById = JSON.parse(", artifact.JavaScript);
        // The user code must not appear as raw JS in the bootstrap; it lives inside the
        // JSON string literal passed to JSON.parse.
        Assert.DoesNotContain("document.title = 'x';", artifact.JavaScript);
    }

    [Fact]
    public void Build_TemplateLiteralHostileFields_DoNotBreakSyntax()
    {
        // Names and code with backticks, ${...}, </script>, U+2028, etc. used to be a
        // worry because they sat inside a C# raw string template. With JSON.parse, all
        // of these go through two layers of JSON escaping and cannot break the bootstrap.
        var hostile = "var a=`tmpl${1}`;\u2028\u2029</script><!--alert(1)-->";
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Name = "name`with`${injection}",
                Code = hostile
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.DoesNotContain("</script>", artifact.JavaScript);
        Assert.DoesNotContain("\u2028", artifact.JavaScript);
        Assert.DoesNotContain("\u2029", artifact.JavaScript);
        Assert.DoesNotContain("`tmpl${1}`", artifact.JavaScript);
    }

    [Fact]
    public void Build_DoesNotEmbedNonceInsideRulesArray()
    {
        // Nonces must travel via a separate map (noncesById) populated by closure capture
        // so a user script cannot read another script's nonce by inspecting `rules`.
        var id = Guid.NewGuid();
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Id = id,
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_setValue"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder("super-secret-nonce").Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("super-secret-nonce", artifact.JavaScript);
        // Property name `nonce` should not appear inside the JSON-encoded rules; we look
        // for the JSON-escaped key as it would appear in the embedded literal.
        Assert.DoesNotContain("\\\"nonce\\\":", artifact.JavaScript);
    }

    [Fact]
    public void Build_UsesNewFunctionInsteadOfEval()
    {
        // `new Function` runs the user code in global scope (parameters + globals only),
        // preventing access to the bootstrap IIFE's locals (rules, noncesById,
        // pendingByRequest, _origPost, ...). This is the second half of the nonce
        // isolation fix.
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("new Function('GM', 'unsafeWindow', rule.code)", artifact.JavaScript);
        Assert.DoesNotContain("eval(code)", artifact.JavaScript);
    }

    [Fact]
    public void Build_DocumentEnd_IncludesDomContentLoaded()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                RunAt = UserScriptRunAt.DocumentEnd,
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("DOMContentLoaded", artifact.JavaScript);
        Assert.Contains("document-end", artifact.JavaScript);
    }

    [Fact]
    public void Build_TopFrameOnly_SkipsChildFrames()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                RunInTopFrameOnly = true,
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("runInTopFrameOnly", artifact.JavaScript);
        Assert.Contains("window.top", artifact.JavaScript);
    }

    [Fact]
    public void Build_DefinesFrozenLegacyBridge()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("Object.defineProperty(window, '__wv2browser'", artifact.JavaScript);
        Assert.Contains("deprecated", artifact.JavaScript);
        Assert.Contains("buildGm", artifact.JavaScript);
    }

    [Fact]
    public void Build_GrantNone_DoesNotInstantiateGm()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["none"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("shouldBuildGm", artifact.JavaScript);
    }

    [Fact]
    public void Build_GrantedScript_ImplementsGmSetValue()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_setValue"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("api.GM_setValue", artifact.JavaScript);
        Assert.Contains("gm.setValue", artifact.JavaScript);
    }

    [Fact]
    public void Build_WithPreloadedValues_EmbedsInitialValues()
    {
        var id = Guid.NewGuid();
        using var valueDoc = JsonDocument.Parse("\"v1\"");
        var preloaded = new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>
        {
            [id] = new Dictionary<string, JsonElement> { ["key1"] = valueDoc.RootElement.Clone() }
        };

        var scripts = new[]
        {
            new UserScriptEntry
            {
                Id = id,
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_getValue"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts, preloaded);

        Assert.NotNull(artifact);
        // After the JSON.parse defense-in-depth wrap, the rules JSON appears as a JS
        // string literal, so the original `"key1"` quotes become `\"key1\"` in the source.
        // Substring search on the unquoted token still proves the preload data is embedded.
        Assert.Contains("key1", artifact.JavaScript);
        Assert.Contains("v1", artifact.JavaScript);
        Assert.Contains("initialValues", artifact.JavaScript);
    }

    [Fact]
    public void Build_GmGetValueGrant_GeneratesGetter()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_getValue"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("api.GM_getValue", artifact.JavaScript);
    }

    [Fact]
    public void Build_InstallsWv2Dispatch()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("__wv2dispatch", artifact.JavaScript);
        Assert.Contains("pendingByRequest", artifact.JavaScript);
        Assert.Contains("dispatchFromHost", artifact.JavaScript);
    }

    [Fact]
    public void Build_GmXhrGrant_GeneratesFunction()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_xmlhttpRequest"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("api.GM_xmlhttpRequest", artifact.JavaScript);
        Assert.Contains("gm.xhrRequest", artifact.JavaScript);
        Assert.DoesNotContain("GM_xmlhttpRequest' is not implemented", artifact.JavaScript);
    }

    [Fact]
    public void Build_GmAddStyleGrant_GeneratesPureJsImpl()
    {
        var scripts = new[]
        {
            new UserScriptEntry
            {
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_addStyle"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts);

        Assert.NotNull(artifact);
        Assert.Contains("api.GM_addStyle", artifact.JavaScript);
        Assert.Contains("createElement('style')", artifact.JavaScript);
    }

    [Fact]
    public void Build_NoStorageGrant_DoesNotEmbedPreloadedValues()
    {
        var id = Guid.NewGuid();
        using var valueDoc = JsonDocument.Parse("\"v1\"");
        var preloaded = new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>
        {
            [id] = new Dictionary<string, JsonElement> { ["key1"] = valueDoc.RootElement.Clone() }
        };

        var scripts = new[]
        {
            new UserScriptEntry
            {
                Id = id,
                Enabled = true,
                MatchPatterns = ["*://*/*"],
                Grants = ["GM_log"],
                Code = "void 0;"
            }
        };

        var artifact = CreateBuilder().Build(scripts, preloaded);

        Assert.NotNull(artifact);
        Assert.DoesNotContain("key1", artifact.JavaScript);
        Assert.DoesNotContain("v1", artifact.JavaScript);
    }
}

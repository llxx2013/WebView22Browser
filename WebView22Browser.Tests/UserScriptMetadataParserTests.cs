using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptMetadataParserTests
{
    [Fact]
    public void Parse_StandardBlock_ExtractsMetadataAndCode()
    {
        const string source = """
            // ==UserScript==
            // @name         Bing Helper
            // @match        *://*.bing.com/*
            // @match        https://example.com/*
            // @run-at       document-idle
            // ==/UserScript==

            console.log('body');
            """;

        var result = UserScriptMetadataParser.Parse(source);

        Assert.Equal("Bing Helper", result.Name);
        Assert.Equal(2, result.MatchPatterns.Length);
        Assert.Equal(UserScriptRunAt.DocumentIdle, result.RunAt);
        Assert.Contains("console.log('body');", result.Code);
        Assert.DoesNotContain("==UserScript==", result.Code);
    }

    [Fact]
    public void Parse_Noframes_SetsTopFrameOnly()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @noframes
            // ==/UserScript==
            alert(1);
            """;

        var result = UserScriptMetadataParser.Parse(source);

        Assert.True(result.RunInTopFrameOnly);
    }

    [Fact]
    public void Parse_Exclude_CollectsPatterns()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*.example.com/*
            // @exclude *://*.example.com/admin/*
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Single(result.ExcludePatterns);
        Assert.Contains("admin", result.ExcludePatterns[0]);
    }

    [Fact]
    public void Parse_NoMetadataBlock_TreatsWholeFileAsCode()
    {
        const string source = "console.log('raw');";
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Equal(source, result.Code);
        Assert.Empty(result.MatchPatterns);
        Assert.Contains("未找到", result.Warnings[0]);
    }

    [Fact]
    public void Parse_InvalidRunAt_FallsBackAndWarns()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @run-at       someday
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Equal(UserScriptRunAt.DocumentStart, result.RunAt);
        Assert.Contains(result.Warnings, w => w.Contains("run-at"));
    }

    [Fact]
    public void Parse_Grant_RecognizesKnownGrant()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @grant GM_xmlhttpRequest
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Contains("GM_xmlhttpRequest", result.Grants);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("grant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_GrantNone_ClearsGrants()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @grant GM_setValue
            // @grant none
            // @grant GM_getValue
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Empty(result.Grants);
    }

    [Fact]
    public void Parse_Connect_CollectsPatterns()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @connect example.com
            // @connect *.other.org
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Equal(2, result.ConnectPatterns.Length);
        Assert.Contains("example.com", result.ConnectPatterns);
        Assert.Contains("*.other.org", result.ConnectPatterns);
        Assert.DoesNotContain(result.Warnings, w => w.Contains("@connect"));
    }

    [Fact]
    public void Parse_UnknownGrant_WarnsButCollects()
    {
        const string source = """
            // ==UserScript==
            // @name Test
            // @match *://*/*
            // @grant GM_customFutureApi
            // ==/UserScript==
            """;
        var result = UserScriptMetadataParser.Parse(source);

        Assert.Contains("GM_customFutureApi", result.Grants);
        Assert.Contains(result.Warnings, w => w.Contains("GM_customFutureApi"));
    }
}
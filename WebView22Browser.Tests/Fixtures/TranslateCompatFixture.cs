using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests.Fixtures;

/// <summary>
/// Trimmed translate.user.js patterns for offline bootstrap compatibility tests (no CDN).
/// Upstream: <see href="https://github.com/syhyz1990/translate">syhyz1990/translate</see> (MIT License).
/// </summary>
internal static class TranslateCompatFixture
{
    public const string MetadataBlock = """
        // ==UserScript==
        // @name              translate-compat-fixture
        // @match             *://*/*
        // @require           https://unpkg.com/jquery@3.7.0/dist/jquery.min.js
        // @require           https://unpkg.com/sweetalert2@10.16.6/dist/sweetalert2.min.js
        // @require           https://unpkg.com/hotkeys-js@3.13.3/dist/hotkeys.min.js
        // @resource          swalStyle https://unpkg.com/sweetalert2@10.16.6/dist/sweetalert2.min.css
        // @run-at            document-idle
        // @noframes
        // @grant             GM_setValue
        // @grant             GM_getValue
        // @grant             GM_xmlhttpRequest
        // @grant             GM_registerMenuCommand
        // @grant             GM_getResourceText
        // ==/UserScript==
        """;

    public static readonly string[] Grants =
    [
        "GM_setValue",
        "GM_getValue",
        "GM_xmlhttpRequest",
        "GM_registerMenuCommand",
        "GM_getResourceText"
    ];

    public static UserScriptEntry CreateEntry() => new()
    {
        Name = "translate-compat-fixture",
        Enabled = true,
        MatchPatterns = ["*://*/*"],
        RunAt = UserScriptRunAt.DocumentIdle,
        RunInTopFrameOnly = true,
        Grants = Grants,
        RequireUrls =
        [
            "https://unpkg.com/jquery@3.7.0/dist/jquery.min.js",
            "https://unpkg.com/sweetalert2@10.16.6/dist/sweetalert2.min.js",
            "https://unpkg.com/hotkeys-js@3.13.3/dist/hotkeys.min.js"
        ],
        Resources = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["swalStyle"] = "https://unpkg.com/sweetalert2@10.16.6/dist/sweetalert2.min.css"
        },
        Code = """
            var Swal = { mixin: function () { return { fire: function () {} }; } };
            var $ = function () { return { on: function () {} }; };
            var hotkeys = function () {};
            var util = {
                getValue: function (name) { return GM_getValue(name); },
                setValue: function (name, value) { GM_setValue(name, value); }
            };
            function registerMenuCommand() {
                GM_registerMenuCommand('fixture-menu', function () {});
            }
            function requestTranslate() {
                GM_xmlhttpRequest({
                    method: 'GET',
                    url: 'https://example.test/api',
                    responseType: 'json',
                    onload: function (res) {
                        if (res.response && res.response.code === 200) {
                            util.setValue('ok', res.response.data);
                        }
                    }
                });
            }
            if (document.head) {
                GM_getResourceText('swalStyle');
            }
            registerMenuCommand();
            requestTranslate();
            """
    };

    public static ResolvedScriptDependencies CreateResolvedDependencies() =>
        new()
        {
            RequireScripts =
            [
                "window.__jqueryMock = 1;",
                "window.__swalMock = 1;",
                "window.__hotkeysMock = 1;"
            ],
            ResourceTexts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["swalStyle"] = ".swal { color: red; }"
            }
        };

    public static UserScriptParseResult ParseMetadata() =>
        UserScriptMetadataParser.Parse(MetadataBlock + Environment.NewLine + CreateEntry().Code);
}
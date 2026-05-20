using System.Security.Cryptography;
using System.Text.Json;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public sealed class UserScriptBootstrapBuilder
{
    private readonly Func<string> _nonceFactory;

    public UserScriptBootstrapBuilder()
        : this(DefaultNonceFactory)
    {
    }

    public UserScriptBootstrapBuilder(Func<string> nonceFactory) =>
        _nonceFactory = nonceFactory;

    public BootstrapArtifact? Build(IEnumerable<UserScriptEntry> scripts) =>
        Build(scripts, new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>());

    public BootstrapArtifact? Build(
        IEnumerable<UserScriptEntry> scripts,
        IReadOnlyDictionary<Guid, IReadOnlyDictionary<string, JsonElement>> preloadedValues)
    {
        var enabled = scripts.Where(s => s.Enabled).ToList();
        if (enabled.Count == 0)
            return null;

        var noncesByScriptId = new Dictionary<Guid, string>();
        var rules = new List<BootstrapRule>();

        foreach (var script in enabled)
        {
            var nonce = _nonceFactory();
            noncesByScriptId[script.Id] = nonce;

            Dictionary<string, JsonElement> initialDict;
            if (UserScriptGrantHelper.HasStorageGrant(script.Grants)
                && preloadedValues.TryGetValue(script.Id, out var loaded))
            {
                initialDict = loaded.ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value,
                    StringComparer.Ordinal);
            }
            else
            {
                initialDict = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            }

            var scriptInfo = new Dictionary<string, object>
            {
                ["script"] = new Dictionary<string, object>
                {
                    ["name"] = script.Name,
                    ["version"] = "0.0.0",
                    ["description"] = "",
                    ["namespace"] = script.Id.ToString()
                }
            };

            rules.Add(new BootstrapRule(
                script.Id.ToString(),
                script.MatchPatterns,
                script.ExcludePatterns,
                script.ConnectPatterns,
                script.Code,
                ToRunAtString(script.RunAt),
                script.RunInTopFrameOnly,
                script.Grants,
                initialDict,
                scriptInfo));
        }

        var rulesJson = JsonSerializer.Serialize(rules);
        var noncesById = noncesByScriptId.ToDictionary(
            static pair => pair.Key.ToString(),
            static pair => pair.Value,
            StringComparer.Ordinal);
        var noncesJson = JsonSerializer.Serialize(noncesById);

        // Defense-in-depth: embed JSON as a JavaScript string literal and parse it at runtime
        // with JSON.parse. This prevents any chance of injection via user-controlled fields
        // (script name, code, etc.) even if a future change altered the surrounding template.
        var rulesJsLiteral = JsonSerializer.Serialize(rulesJson);
        var noncesJsLiteral = JsonSerializer.Serialize(noncesJson);

        var javascript = $$"""
            (function () {
              if (window.__wv2browserBootstrap) return;
              window.__wv2browserBootstrap = true;

              var pendingByRequest = {};
              var pendingByMenuCommand = {};
              function dispatchFromHost(payload) {
                try {
                  if (!payload) return;
                  if (payload.kind === 'menuClick'
                    && typeof payload.scriptId === 'string'
                    && typeof payload.commandId === 'number') {
                    var menuKey = payload.scriptId + ':' + payload.commandId;
                    var menuEntry = pendingByMenuCommand[menuKey];
                    if (!menuEntry || menuEntry.scriptId !== payload.scriptId) return;
                    menuEntry.handler();
                    return;
                  }
                  if (typeof payload.requestId !== 'string') return;
                  var entry = pendingByRequest[payload.requestId];
                  if (!entry) return;
                  if (entry.scriptId !== payload.scriptId) return;
                  entry.handler(payload);
                } catch (e) { console.error('[userscript] dispatch', e); }
              }
              Object.defineProperty(window, '__wv2dispatch', {
                value: dispatchFromHost, writable: false, configurable: false
              });
              try {
                window.addEventListener('pagehide', function () {
                  for (var k in pendingByRequest) {
                    if (Object.prototype.hasOwnProperty.call(pendingByRequest, k)) {
                      delete pendingByRequest[k];
                    }
                  }
                  for (var mk in pendingByMenuCommand) {
                    if (Object.prototype.hasOwnProperty.call(pendingByMenuCommand, mk)) {
                      delete pendingByMenuCommand[mk];
                    }
                  }
                }, { once: true });
              } catch (e) {}

              var _origPost = (window.chrome && window.chrome.webview)
                ? window.chrome.webview.postMessage.bind(window.chrome.webview) : null;
              var legacyWarned = false;

              function postLegacy(type, payload) {
                if (!_origPost) return;
                if (!legacyWarned) {
                  legacyWarned = true;
                  try { console.warn('[wv2browser] window.__wv2browser is deprecated; use GM_* (see @grant).'); } catch (e) {}
                }
                _origPost(JSON.stringify({ type: type, payload: payload || {} }));
              }

              var legacy = Object.freeze({
                notify: function (message) { postLegacy('notify', { message: String(message) }); },
                log: function (message) { postLegacy('log', { message: String(message) }); }
              });
              Object.defineProperty(window, '__wv2browser', { value: legacy, writable: false, configurable: false });

              function buildGm(scriptId, nonce, grants, initialValues, scriptInfo) {
                function postGm(type, payload) {
                  if (!_origPost) return;
                  _origPost(JSON.stringify({
                    type: type,
                    scriptId: scriptId,
                    nonce: nonce,
                    payload: payload || {}
                  }));
                }
                var values = {};
                var initKeys = Object.keys(initialValues || {});
                for (var ik = 0; ik < initKeys.length; ik++) {
                  var initKey = initKeys[ik];
                  values[initKey] = initialValues[initKey];
                }
                var api = {};
                var grantSet = {};
                for (var gi = 0; gi < grants.length; gi++) grantSet[grants[gi]] = true;
                if (grantSet['GM_setValue']) {
                  api.GM_setValue = function (key, value) {
                    if (typeof key !== 'string') throw new Error('GM_setValue: key must be string');
                    values[key] = value;
                    postGm('gm.setValue', { key: key, value: value });
                  };
                }
                if (grantSet['GM_getValue']) {
                  api.GM_getValue = function (key, defaultValue) {
                    return Object.prototype.hasOwnProperty.call(values, key) ? values[key] : defaultValue;
                  };
                }
                if (grantSet['GM_deleteValue']) {
                  api.GM_deleteValue = function (key) {
                    if (typeof key !== 'string') throw new Error('GM_deleteValue: key must be string');
                    delete values[key];
                    postGm('gm.deleteValue', { key: key });
                  };
                }
                if (grantSet['GM_listValues']) {
                  api.GM_listValues = function () { return Object.keys(values); };
                }
                if (grantSet['GM_log']) {
                  api.GM_log = function (m) { postGm('gm.log', { message: String(m) }); };
                }
                if (grantSet['GM_info']) {
                  api.GM_info = scriptInfo;
                }
                if (grantSet['GM_notification']) {
                  api.GM_notification = function (text, title) {
                    var details = (typeof text === 'object' && text !== null) ? text : { text: String(text), title: title };
                    postGm('gm.notification', { text: details.text, title: details.title || '' });
                  };
                }
                if (grantSet['GM_addStyle']) {
                  api.GM_addStyle = function (css) {
                    var style = document.createElement('style');
                    style.textContent = String(css);
                    (document.head || document.documentElement).appendChild(style);
                    return style;
                  };
                }
                if (grantSet['GM_openInTab']) {
                  api.GM_openInTab = function (url, options) {
                    var active = true;
                    if (typeof options === 'object' && options !== null && options.active === false) active = false;
                    postGm('gm.openInTab', { url: String(url), active: active });
                    return null;
                  };
                }
                if (grantSet['GM_setClipboard']) {
                  api.GM_setClipboard = function (data, type) {
                    var clipType = type || 'text';
                    if (clipType !== 'text') {
                      throw new Error('[wv2browser] GM_setClipboard: only text type is supported');
                    }
                    postGm('gm.setClipboard', { data: String(data), type: clipType });
                  };
                }
                if (grantSet['GM_xmlhttpRequest']) {
                  api.GM_xmlhttpRequest = function (details) {
                    if (!details || typeof details.url !== 'string') throw new Error('GM_xmlhttpRequest: url required');
                    var rid = (typeof crypto !== 'undefined' && crypto.randomUUID)
                      ? crypto.randomUUID()
                      : ('rid-' + Date.now() + '-' + Math.random().toString(36).slice(2));
                    pendingByRequest[rid] = {
                      scriptId: scriptId,
                      handler: function (payload) {
                        try {
                          if (payload.kind === 'load') {
                            if (details.onload) details.onload(payload.response);
                          } else if (payload.kind === 'error') {
                            if (details.onerror) details.onerror({ error: payload.error, details: payload });
                          } else if (payload.kind === 'timeout') {
                            if (details.ontimeout) details.ontimeout({});
                          } else if (payload.kind === 'abort') {
                            if (details.onabort) details.onabort({});
                          }
                        } finally {
                          if (payload.kind !== 'progress') delete pendingByRequest[rid];
                        }
                      }
                    };
                    postGm('gm.xhrRequest', {
                      requestId: rid,
                      method: (details.method || 'GET').toUpperCase(),
                      url: details.url,
                      headers: details.headers || null,
                      data: typeof details.data === 'string' ? details.data : null,
                      timeoutMs: typeof details.timeout === 'number' ? details.timeout : 0,
                      responseType: details.responseType || 'text'
                    });
                    return {
                      abort: function () { postGm('gm.xhrAbort', { requestId: rid }); }
                    };
                  };
                }
                if (grantSet['GM_registerMenuCommand']) {
                  var menuCommandSeq = 0;
                  api.GM_registerMenuCommand = function (caption, onClick, accessKey) {
                    if (typeof caption !== 'string') throw new Error('GM_registerMenuCommand: caption must be string');
                    if (typeof onClick !== 'function') throw new Error('GM_registerMenuCommand: onClick must be function');
                    menuCommandSeq += 1;
                    var commandId = menuCommandSeq;
                    var menuKey = scriptId + ':' + commandId;
                    pendingByMenuCommand[menuKey] = {
                      scriptId: scriptId,
                      handler: function () {
                        try { onClick(); } catch (e) { console.error('[userscript]', scriptId, e); }
                      }
                    };
                    postGm('gm.registerMenuCommand', {
                      commandId: commandId,
                      caption: caption,
                      accessKey: accessKey != null && accessKey !== '' ? String(accessKey) : ''
                    });
                    return commandId;
                  };
                  api.GM_unregisterMenuCommand = function (commandId) {
                    if (typeof commandId !== 'number') return;
                    var unregisterKey = scriptId + ':' + commandId;
                    delete pendingByMenuCommand[unregisterKey];
                    postGm('gm.unregisterMenuCommand', { commandId: commandId });
                  };
                }
                return Object.freeze(api);
              }

              function segmentMatches(value, pattern) {
                if (pattern === '*') return true;
                if (pattern.indexOf('*') < 0) return value.toLowerCase() === pattern.toLowerCase();
                if (pattern.charAt(0) === '*' && pattern.charAt(pattern.length - 1) === '*' && pattern.length >= 2) {
                  return value.toLowerCase().indexOf(pattern.slice(1, -1).toLowerCase()) >= 0;
                }
                if (pattern.charAt(0) === '*') return value.toLowerCase().endsWith(pattern.slice(1).toLowerCase());
                if (pattern.charAt(pattern.length - 1) === '*') return value.toLowerCase().startsWith(pattern.slice(0, -1).toLowerCase());
                var parts = pattern.split('*');
                var index = 0;
                for (var i = 0; i < parts.length; i++) {
                  var part = parts[i];
                  if (!part) continue;
                  var found = value.toLowerCase().indexOf(part.toLowerCase(), index);
                  if (found < 0) return false;
                  if (i === 0 && pattern.charAt(0) !== '*' && found !== 0) return false;
                  index = found + part.length;
                }
                if (pattern.charAt(pattern.length - 1) !== '*' && index !== value.length) return false;
                return true;
              }

              function parsePattern(pattern) {
                var schemeEnd = pattern.indexOf('://');
                if (schemeEnd < 0) return null;
                var scheme = pattern.slice(0, schemeEnd);
                var remainder = pattern.slice(schemeEnd + 3);
                var pathStart = remainder.indexOf('/');
                if (pathStart < 0) return { scheme: scheme, host: remainder, path: '/*' };
                return { scheme: scheme, host: remainder.slice(0, pathStart), path: remainder.slice(pathStart) };
              }

              function isRestrictedScheme(scheme) {
                var s = scheme.toLowerCase();
                return s === 'file' || s === 'about' || s === 'edge';
              }

              function patternAllowsScheme(pattern, scheme) {
                var schemeEnd = pattern.indexOf('://');
                if (schemeEnd < 0) return false;
                var patternScheme = pattern.slice(0, schemeEnd);
                return patternScheme === '*' || patternScheme.toLowerCase() === scheme.toLowerCase();
              }

              function matchUrl(url, pattern) {
                if (!url || !pattern) return false;
                try {
                  var u = new URL(url);
                  var parsed = parsePattern(pattern.trim());
                  if (!parsed) return false;
                  var scheme = u.protocol.replace(':', '');
                  if (isRestrictedScheme(scheme)) {
                    if (!patternAllowsScheme(pattern, scheme)) return false;
                    var slash = pattern.indexOf('://');
                    if (slash >= 0 && pattern.slice(0, slash) === '*') return false;
                  }
                  if (!segmentMatches(u.protocol.replace(':', ''), parsed.scheme)) return false;
                  if (!segmentMatches(u.hostname, parsed.host)) return false;
                  var uriPath = u.pathname || '/';
                  if (scheme === 'file' && uriPath.charAt(0) !== '/') uriPath = '/' + uriPath;
                  if (u.search) uriPath += u.search;
                  return segmentMatches(uriPath, parsed.path);
                } catch (e) { return false; }
              }

              function matchAny(url, patterns) {
                if (!patterns || patterns.length === 0) return false;
                for (var i = 0; i < patterns.length; i++) {
                  if (matchUrl(url, patterns[i])) return true;
                }
                return false;
              }

              function shouldBuildGm(grants) {
                if (!grants || grants.length === 0) return false;
                for (var i = 0; i < grants.length; i++) {
                  if (grants[i] === 'none') return false;
                }
                return true;
              }

              function runUserScript(rule) {
                var nonce = noncesById[rule.id];
                var gm = shouldBuildGm(rule.grants)
                  ? buildGm(rule.id, nonce, rule.grants, rule.initialValues, rule.info)
                  : null;
                // Use `new Function` instead of `eval` so the user code cannot reach the
                // bootstrap IIFE scope (rules array, noncesById, pendingByRequest, _origPost,
                // etc.). `new Function` always parses in global scope; only the declared
                // parameters and globals are reachable from the code body.
                var exec = function () {
                  try {
                    var userFn;
                    try {
                      userFn = new Function('GM', 'unsafeWindow', rule.code);
                    } catch (compileErr) {
                      console.error('[userscript]', rule.id, compileErr);
                      return;
                    }
                    userFn.call(window, gm, window);
                  } catch (e) {
                    console.error('[userscript]', rule.id, e);
                  }
                };
                if (rule.runAt === 'document-end') {
                  if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', exec, { once: true });
                  } else {
                    exec();
                  }
                  return;
                }
                if (rule.runAt === 'document-idle') {
                  var idleRun = function () {
                    if (typeof requestIdleCallback === 'function') {
                      requestIdleCallback(exec, { timeout: 2000 });
                    } else {
                      setTimeout(exec, 0);
                    }
                  };
                  if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', idleRun, { once: true });
                  } else {
                    idleRun();
                  }
                  return;
                }
                exec();
              }

              var rules = JSON.parse({{rulesJsLiteral}});
              var noncesById = JSON.parse({{noncesJsLiteral}});
              var href = location.href;
              for (var i = 0; i < rules.length; i++) {
                var rule = rules[i];
                if (rule.runInTopFrameOnly && window !== window.top) continue;
                if (!matchAny(href, rule.matchPatterns)) continue;
                if (rule.excludePatterns && rule.excludePatterns.length > 0 && matchAny(href, rule.excludePatterns)) continue;
                runUserScript(rule);
              }
            })();
            """;

        return new BootstrapArtifact
        {
            JavaScript = javascript,
            NoncesByScriptId = noncesByScriptId
        };
    }

    private static string DefaultNonceFactory()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string ToRunAtString(UserScriptRunAt runAt) =>
        runAt switch
        {
            UserScriptRunAt.DocumentEnd => "document-end",
            UserScriptRunAt.DocumentIdle => "document-idle",
            _ => "document-start"
        };

    // Nonce intentionally omitted: it is kept in a separate map populated via closure
    // (noncesById) so user-script code cannot read other scripts' nonces by inspecting
    // the `rules` array.
    private sealed record BootstrapRule(
        string Id,
        string[] MatchPatterns,
        string[] ExcludePatterns,
        string[] ConnectPatterns,
        string Code,
        string RunAt,
        bool RunInTopFrameOnly,
        string[] Grants,
        Dictionary<string, JsonElement> InitialValues,
        Dictionary<string, object> Info);
}

using System.Diagnostics;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core;
using WebView22Browser.Core.Async;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Controls;

/// <summary>
/// HTTPS security state, certificate errors, and permission prompts.
/// </summary>
public partial class TabWebViewHost
{
    private bool _securityDevToolsEnabled;
    private CoreWebView2DevToolsProtocolEventReceiver? _securityEventReceiver;
    private EventHandler<CoreWebView2DevToolsProtocolEventReceivedEventArgs>? _securityStateHandler;

    private void UnwireSecurityMonitoring(CoreWebView2 core)
    {
        if (_securityEventReceiver != null && _securityStateHandler != null)
            _securityEventReceiver.DevToolsProtocolEventReceived -= _securityStateHandler;

        _securityDevToolsEnabled = false;
        _securityEventReceiver = null;
        _securityStateHandler = null;
    }

    private async Task EnableSecurityMonitoringAsync(CoreWebView2 core)
    {
        if (_securityDevToolsEnabled || Tab == null)
            return;

        try
        {
            await core.CallDevToolsProtocolMethodAsync("Security.enable", "{}");
            _securityEventReceiver = core.GetDevToolsProtocolEventReceiver("Security.visibleSecurityStateChanged");
            _securityStateHandler = OnVisibleSecurityStateChanged;
            _securityEventReceiver.DevToolsProtocolEventReceived += _securityStateHandler;
            _securityDevToolsEnabled = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WebView22Browser] Security.enable failed: {ex.Message}");
            ApplySchemeSecurityFallback();
        }
    }

    private void OnVisibleSecurityStateChanged(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        if (Tab == null)
            return;

        Tab.SecurityState = SecurityStateDevToolsParser.ParseVisibleSecurityStateChanged(e.ParameterObjectAsJson);
    }

    private void ApplySchemeSecurityFallback()
    {
        if (Tab == null)
            return;

        Tab.SecurityState = SecurityStateDevToolsParser.FromUriScheme(webView.Source?.ToString());
    }

    private void OnServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        if (DialogService == null)
        {
            e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
            if (Tab != null)
                Tab.SecurityState = AddressBarSecurityState.Dangerous;
            return;
        }

        // WebView2 only exposes AlwaysAllow (session-scoped cache) or Cancel — no per-navigation Allow.
        var allow = DialogService.PromptCertificateError(e.RequestUri, e.ErrorStatus.ToString());
        e.Action = allow
            ? CoreWebView2ServerCertificateErrorAction.AlwaysAllow
            : CoreWebView2ServerCertificateErrorAction.Cancel;

        if (!allow && Tab != null)
            Tab.SecurityState = AddressBarSecurityState.Dangerous;
    }

    private void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e) =>
        FireAndForget.Run(() => OnPermissionRequestedAsync(sender, e), ReportPermissionFailure);

    private async Task OnPermissionRequestedAsync(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (PermissionStore != null &&
            PermissionStore.TryGet(e.Uri, e.PermissionKind, out var remembered))
        {
            e.State = remembered;
            return;
        }

        if (DialogService == null)
        {
            e.State = CoreWebView2PermissionState.Deny;
            return;
        }

        var allow = DialogService.PromptPermission(e.PermissionKind, e.Uri);
        e.State = allow ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;

        if (PermissionStore != null)
            await PermissionStore.SetAsync(e.Uri, e.PermissionKind, e.State);
    }

    private void ReportPermissionFailure(Exception ex) =>
        Trace.WriteLine($"[WebView22Browser] Permission request failed: {ex.Message}");
}
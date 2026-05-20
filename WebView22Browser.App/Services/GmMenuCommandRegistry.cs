using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Services;

public sealed class GmMenuCommandRegistry
{
    private readonly ConcurrentDictionary<CoreWebView2, object> _locks = new();
    private readonly ConcurrentDictionary<CoreWebView2, List<GmMenuCommandDescriptor>> _commandsByWebView = new();

    public event EventHandler<CoreWebView2>? CommandsChanged;

    public void Register(CoreWebView2 core, GmMenuCommandDescriptor descriptor)
    {
        var list = GetOrCreateList(core);
        lock (GetLock(core))
        {
            list.RemoveAll(c =>
                c.CommandId == descriptor.CommandId && c.ScriptId == descriptor.ScriptId);
            list.Add(descriptor);
        }

        CommandsChanged?.Invoke(this, core);
    }

    public void Unregister(CoreWebView2 core, Guid scriptId, int commandId)
    {
        if (!_commandsByWebView.TryGetValue(core, out var list))
            return;

        lock (GetLock(core))
        {
            list.RemoveAll(c => c.CommandId == commandId && c.ScriptId == scriptId);
        }

        CommandsChanged?.Invoke(this, core);
    }

    public void Clear(CoreWebView2 core)
    {
        if (_commandsByWebView.TryGetValue(core, out var list))
        {
            lock (GetLock(core))
                list.Clear();
        }

        CommandsChanged?.Invoke(this, core);

        _commandsByWebView.TryRemove(core, out _);
        _locks.TryRemove(core, out _);
    }

    public IReadOnlyList<GmMenuCommandDescriptor> GetCommands(CoreWebView2 core)
    {
        if (!_commandsByWebView.TryGetValue(core, out var list))
            return Array.Empty<GmMenuCommandDescriptor>();

        lock (GetLock(core))
            return list.ToList();
    }

    public async Task InvokeAsync(CoreWebView2 core, int commandId, Guid scriptId)
    {
        if (!_commandsByWebView.TryGetValue(core, out var list))
            return;

        GmMenuCommandDescriptor? match;
        lock (GetLock(core))
        {
            match = list.FirstOrDefault(c => c.CommandId == commandId && c.ScriptId == scriptId);
        }

        if (match == null)
            return;

        try
        {
            await Wv2GmDispatchChannel.DispatchAsync(core, new
            {
                kind = "menuClick",
                commandId,
                scriptId = scriptId.ToString()
            });
        }
        catch (ObjectDisposedException)
        {
            Debug.WriteLine("[userscript] GM menu command invoke skipped: WebView disposed.");
        }
        catch (InvalidOperationException ex)
        {
            Debug.WriteLine($"[userscript] GM menu command invoke skipped: {ex.Message}");
        }
    }

    private List<GmMenuCommandDescriptor> GetOrCreateList(CoreWebView2 core) =>
        _commandsByWebView.GetOrAdd(core, _ => new List<GmMenuCommandDescriptor>());

    private object GetLock(CoreWebView2 core) =>
        _locks.GetOrAdd(core, _ => new object());
}

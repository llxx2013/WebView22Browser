using System.Runtime.CompilerServices;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Tests;

public sealed class GmMenuCommandRegistryTests
{
    [Fact]
    public void Register_Unregister_GetCommands_WorkPerScript()
    {
        var registry = new GmMenuCommandRegistry();
        var core = CreateStubCore();
        var scriptId = Guid.NewGuid();

        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "Do thing", "D"));

        var commands = registry.GetCommands(core);
        Assert.Single(commands);
        Assert.Equal(1, commands[0].CommandId);
        Assert.Equal("Do thing", commands[0].Caption);
        Assert.Equal("D", commands[0].AccessKey);

        registry.Unregister(core, scriptId, 1);
        Assert.Empty(registry.GetCommands(core));
    }

    [Fact]
    public void Register_ReplacesSameCommandIdForScript()
    {
        var registry = new GmMenuCommandRegistry();
        var core = CreateStubCore();
        var scriptId = Guid.NewGuid();

        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "First", null));
        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "Second", null));

        var commands = registry.GetCommands(core);
        Assert.Single(commands);
        Assert.Equal("Second", commands[0].Caption);
    }

    [Fact]
    public void Clear_RemovesAllCommandsForWebView()
    {
        var registry = new GmMenuCommandRegistry();
        var core = CreateStubCore();
        var scriptId = Guid.NewGuid();

        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "A", null));
        registry.Register(core, new GmMenuCommandDescriptor(2, scriptId, "B", null));

        registry.Clear(core);

        Assert.Empty(registry.GetCommands(core));
    }

    [Fact]
    public void CommandsChanged_FiresOnRegisterUnregisterAndClear()
    {
        var registry = new GmMenuCommandRegistry();
        var core = CreateStubCore();
        var scriptId = Guid.NewGuid();
        var changeCount = 0;

        registry.CommandsChanged += (_, changedCore) =>
        {
            changeCount++;
            Assert.Same(core, changedCore);
        };

        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "A", null));
        registry.Unregister(core, scriptId, 1);
        registry.Clear(core);

        Assert.Equal(3, changeCount);
    }

    [Fact]
    public void Clear_EmptiesListBeforeRemovingWebViewEntry()
    {
        var registry = new GmMenuCommandRegistry();
        var core = CreateStubCore();
        var scriptId = Guid.NewGuid();
        var observedCountDuringEvent = -1;

        registry.Register(core, new GmMenuCommandDescriptor(1, scriptId, "A", null));
        registry.CommandsChanged += (_, changedCore) =>
        {
            if (ReferenceEquals(changedCore, core))
                observedCountDuringEvent = registry.GetCommands(core).Count;
        };

        registry.Clear(core);

        Assert.Equal(0, observedCountDuringEvent);
        Assert.Empty(registry.GetCommands(core));
    }

    private static CoreWebView2 CreateStubCore()
    {
        // Registry only uses CoreWebView2 as dictionary key; no WebView2 runtime required.
        return (CoreWebView2)RuntimeHelpers.GetUninitializedObject(typeof(CoreWebView2));
    }
}
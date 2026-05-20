namespace WebView22Browser.App.ViewModels;

public sealed class UserScriptMenuCommandItemViewModel
{
    public required int CommandId { get; init; }

    public required Guid ScriptId { get; init; }

    public required string DisplayName { get; init; }
}
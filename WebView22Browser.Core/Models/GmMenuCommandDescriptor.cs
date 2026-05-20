namespace WebView22Browser.Core.Models;

public sealed record GmMenuCommandDescriptor(
    int CommandId,
    Guid ScriptId,
    string Caption,
    string? AccessKey);
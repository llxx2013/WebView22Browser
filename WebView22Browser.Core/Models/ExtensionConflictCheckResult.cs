namespace WebView22Browser.Core.Models;

public sealed class ExtensionConflictCheckResult
{
    public static ExtensionConflictCheckResult Unavailable(string reason) =>
        new() { IsAvailable = false, UnavailableReason = reason, Conflicts = [] };

    public static ExtensionConflictCheckResult Available(IReadOnlyList<UserScriptConflict> conflicts) =>
        new() { IsAvailable = true, Conflicts = conflicts };

    public bool IsAvailable { get; init; }

    public string? UnavailableReason { get; init; }

    public IReadOnlyList<UserScriptConflict> Conflicts { get; init; } = [];
}

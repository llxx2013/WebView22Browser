namespace WebView22Browser.Core.Models;

public sealed class GmStorageQuota
{
    public int MaxValueBytes { get; init; } = 256 * 1024;

    public int MaxScriptBytes { get; init; } = 5 * 1024 * 1024;

    public int MaxKeyLength { get; init; } = 256;
}
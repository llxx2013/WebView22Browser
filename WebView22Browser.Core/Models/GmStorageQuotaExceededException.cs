namespace WebView22Browser.Core.Models;

public sealed class GmStorageQuotaExceededException : Exception
{
    public GmStorageQuotaExceededException(string message)
        : base(message)
    {
    }
}

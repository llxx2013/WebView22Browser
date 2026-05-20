namespace WebView22Browser.Core.Services;

public static class UserScriptGrantHelper
{
    private static readonly HashSet<string> StorageGrants = new(StringComparer.Ordinal)
    {
        "GM_setValue",
        "GM_getValue",
        "GM_deleteValue",
        "GM_listValues"
    };

    public static bool HasStorageGrant(IEnumerable<string> grants) =>
        grants.Any(StorageGrants.Contains);
}

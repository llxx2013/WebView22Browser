namespace WebView22Browser.Core.Services;

public static class UserScriptGrantCatalog
{
    public static readonly HashSet<string> KnownGrants = new(StringComparer.Ordinal)
    {
        "none",
        "unsafeWindow",
        "GM_setValue",
        "GM_getValue",
        "GM_deleteValue",
        "GM_listValues",
        "GM_xmlhttpRequest",
        "GM_log",
        "GM_info",
        "GM_notification",
        "GM_addStyle",
        "GM_openInTab",
        "GM_registerMenuCommand",
        "GM_unregisterMenuCommand",
        "GM_setClipboard"
    };
}
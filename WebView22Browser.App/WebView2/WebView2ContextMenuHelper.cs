using Microsoft.Web.WebView2.Core;

namespace WebView22Browser.App.WebView2;

internal static class WebView2ContextMenuHelper
{
    private static readonly HashSet<string> NewTabMenuItemNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "openLinkInNewTab",
        "openLinkInNewWindow"
    };

    public static void RemoveDefaultOpenInNewTabItems(IList<CoreWebView2ContextMenuItem> menuItems)
    {
        for (var i = menuItems.Count - 1; i >= 0; i--)
        {
            var item = menuItems[i];
            if (NewTabMenuItemNames.Contains(item.Name))
            {
                menuItems.RemoveAt(i);
                continue;
            }

            if (item.Kind == CoreWebView2ContextMenuItemKind.Submenu)
                RemoveDefaultOpenInNewTabItems(item.Children);
        }
    }
}

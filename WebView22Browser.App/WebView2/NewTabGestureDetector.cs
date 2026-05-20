using System.Runtime.InteropServices;

namespace WebView22Browser.App.WebView2;

public static class NewTabGestureDetector
{
    private const int VkControl = 0x11;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static bool IsControlClick() =>
        (GetAsyncKeyState(VkControl) & 0x8000) != 0;
}
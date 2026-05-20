namespace WebView22Browser.Core.Services;

public static class DownloadProgressFormatter
{
    private static readonly string[] SizeUnits = ["B", "KB", "MB", "GB", "TB"];

    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            bytes = 0;

        if (bytes == 0)
            return "0 B";

        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < SizeUnits.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{bytes} B"
            : $"{value:0.##} {SizeUnits[unitIndex]}";
    }

    public static string FormatPercent(long bytesReceived, long totalBytes)
    {
        if (totalBytes <= 0)
            return string.Empty;

        var percent = Math.Clamp(bytesReceived * 100.0 / totalBytes, 0, 100);
        return $"{percent:0}%";
    }

    public static string FormatSpeed(long bytesPerSecond)
    {
        if (bytesPerSecond <= 0)
            return string.Empty;

        return $"{FormatBytes(bytesPerSecond)}/s";
    }

    public static string FormatProgressText(long bytesReceived, long totalBytes)
    {
        if (totalBytes > 0)
            return $"{FormatBytes(bytesReceived)} / {FormatBytes(totalBytes)}";

        return FormatBytes(bytesReceived);
    }

    public static double? GetProgressValue(long bytesReceived, long totalBytes)
    {
        if (totalBytes <= 0)
            return null;

        return Math.Clamp(bytesReceived * 100.0 / totalBytes, 0, 100);
    }
}

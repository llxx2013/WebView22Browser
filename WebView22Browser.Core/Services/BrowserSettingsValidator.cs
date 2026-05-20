using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class BrowserSettingsValidator
{
    public static BrowserSettingsValidationResult Validate(BrowserSettingsDto settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(settings.HomeUrl))
            errors.Add("主页地址不能为空。");
        else if (!IsValidHttpUrl(settings.HomeUrl))
            errors.Add("主页地址必须是有效的 http 或 https URL。");

        if (string.IsNullOrWhiteSpace(settings.SearchUrlTemplate))
            errors.Add("搜索地址模板不能为空。");
        else if (!settings.SearchUrlTemplate.Contains("{0}", StringComparison.Ordinal))
            errors.Add("搜索地址模板必须包含 {0} 占位符。");
        else if (!TryFormatSearchUrl(settings.SearchUrlTemplate, "test"))
            errors.Add("搜索地址模板格式无效。");

        if (settings.TabSleepTimeoutMinutes is null or < 0)
            errors.Add("标签休眠超时不能为负数。");

        if (settings.TabSleepCheckIntervalSeconds is null or < 1)
            errors.Add("休眠扫描间隔至少为 1 秒。");

        if (settings.TabHistoryMaxEntries is null or < 1)
            errors.Add("每标签历史栈上限至少为 1。");

        if (settings.PressureElevatedMemoryPercent is null or < 1 or > 100)
            errors.Add("Elevated 内存阈值须在 1–100 之间。");

        if (settings.PressureHighMemoryPercent is null or < 1 or > 100)
            errors.Add("High 内存阈值须在 1–100 之间。");

        if (settings.PressureElevatedMemoryPercent is int elevated
            && settings.PressureHighMemoryPercent is int high
            && high < elevated)
            errors.Add("High 内存阈值不能低于 Elevated 内存阈值。");

        if (settings.PressureHighCpuPercent is null or < 1 or > 100)
            errors.Add("High CPU 阈值须在 1–100 之间。");

        if (settings.PressureSampleWindowSeconds is null or < 1)
            errors.Add("压力采样窗口至少为 1 秒。");

        if (settings.BrowsingHistoryMaxEntries is null or < 1)
            errors.Add("浏览历史上限至少为 1。");

        if (settings.DownloadHistoryMaxEntries is null or < 1)
            errors.Add("下载历史上限至少为 1。");

        return errors.Count == 0
            ? BrowserSettingsValidationResult.Success
            : new BrowserSettingsValidationResult(errors);
    }

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    private static bool TryFormatSearchUrl(string template, string query)
    {
        try
        {
            var formatted = string.Format(template, Uri.EscapeDataString(query));
            return Uri.TryCreate(formatted, UriKind.Absolute, out _);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

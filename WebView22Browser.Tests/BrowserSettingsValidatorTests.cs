using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowserSettingsValidatorTests
{
    private static BrowserSettingsDto ValidDto() => new()
    {
        HomeUrl = "https://www.bing.com",
        SearchUrlTemplate = "https://www.bing.com/search?q={0}",
        TabSleepTimeoutMinutes = 5,
        TabSleepCheckIntervalSeconds = 30,
        TabHistoryMaxEntries = 50,
        RestoreLastSession = true,
        PressureElevatedMemoryPercent = 70,
        PressureHighMemoryPercent = 85,
        PressureHighCpuPercent = 80,
        PressureSampleWindowSeconds = 15,
        BrowsingHistoryMaxEntries = 2000,
        DownloadHistoryMaxEntries = 200
    };

    [Fact]
    public void Validate_ValidDto_Succeeds()
    {
        var result = BrowserSettingsValidator.Validate(ValidDto());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyHomeUrl_Fails()
    {
        var dto = ValidDto();
        dto.HomeUrl = "  ";
        Assert.False(BrowserSettingsValidator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_SearchTemplateWithoutPlaceholder_Fails()
    {
        var dto = ValidDto();
        dto.SearchUrlTemplate = "https://search.test";
        Assert.False(BrowserSettingsValidator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_HighMemoryBelowElevated_Fails()
    {
        var dto = ValidDto();
        dto.PressureElevatedMemoryPercent = 90;
        dto.PressureHighMemoryPercent = 80;
        Assert.False(BrowserSettingsValidator.Validate(dto).IsValid);
    }

    [Theory]
    [InlineData(nameof(BrowserSettingsDto.TabSleepTimeoutMinutes), -1)]
    [InlineData(nameof(BrowserSettingsDto.TabSleepCheckIntervalSeconds), 0)]
    [InlineData(nameof(BrowserSettingsDto.TabHistoryMaxEntries), 0)]
    [InlineData(nameof(BrowserSettingsDto.BrowsingHistoryMaxEntries), 0)]
    [InlineData(nameof(BrowserSettingsDto.DownloadHistoryMaxEntries), 0)]
    [InlineData(nameof(BrowserSettingsDto.PressureElevatedMemoryPercent), 0)]
    [InlineData(nameof(BrowserSettingsDto.PressureSampleWindowSeconds), 0)]
    public void Validate_OutOfRangeNumeric_Fails(string propertyName, int value)
    {
        var dto = ValidDto();
        typeof(BrowserSettingsDto).GetProperty(propertyName)!.SetValue(dto, value);
        Assert.False(BrowserSettingsValidator.Validate(dto).IsValid);
    }

    [Fact]
    public void Validate_NullNumericFields_Fails()
    {
        var dto = new BrowserSettingsDto
        {
            HomeUrl = "https://www.bing.com",
            SearchUrlTemplate = "https://www.bing.com/search?q={0}"
        };

        Assert.False(BrowserSettingsValidator.Validate(dto).IsValid);
    }
}
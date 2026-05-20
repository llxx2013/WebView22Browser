using System.Globalization;
using System.Windows.Data;

using WebView22Browser.Core.Models;

namespace WebView22Browser.App.Converters;

public sealed class SecurityStateToGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AddressBarSecurityState state)
            return "○";

        return state switch
        {
            AddressBarSecurityState.Secure => "🔒",
            AddressBarSecurityState.Neutral => "○",
            AddressBarSecurityState.Insecure => "⚠",
            AddressBarSecurityState.Dangerous => "🚫",
            _ => "🔍"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class SecurityStateToToolTipConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not AddressBarSecurityState state)
            return "未知连接状态";

        return state switch
        {
            AddressBarSecurityState.Secure => "连接安全",
            AddressBarSecurityState.Neutral => "连接信息",
            AddressBarSecurityState.Insecure => "连接不安全",
            AddressBarSecurityState.Dangerous => "连接存在严重风险（如证书错误）",
            _ => "未知连接状态"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
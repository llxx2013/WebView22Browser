using System.Globalization;
using System.Windows.Data;

namespace WebView22Browser.App.Converters;

public sealed class PanelWidthConverter : IValueConverter
{
    public double OpenWidth { get; set; } = 220;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? OpenWidth : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
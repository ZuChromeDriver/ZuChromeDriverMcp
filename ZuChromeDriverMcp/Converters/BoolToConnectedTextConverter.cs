using System.Globalization;
using System.Windows.Data;

namespace ZuChromeDriverMcp.Converters;

public sealed class BoolToConnectedTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool connected)
            return connected ? "Connected" : "Disconnected";

        return "Disconnected";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

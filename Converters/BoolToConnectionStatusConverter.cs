using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Boolean değeri bağlantı durumu metnine çeviren converter
/// </summary>
public class BoolToConnectionStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected && isConnected)
            return "Bağlı";

        return "Bağlantı Yok";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for BoolToConnectionStatusConverter");
    }
}

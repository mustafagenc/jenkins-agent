using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Boolean değeri tersine çeviren converter
/// </summary>
public class InvertedBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
            return !boolValue;

        return true;
    }
}

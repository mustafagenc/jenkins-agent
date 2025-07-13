using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Sayı değerini Visibility'ye çeviren converter
/// Parameter ile eşitlik karşılaştırması yapılabilir
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return Visibility.Collapsed;

        int count = 0;

        if (value is int intValue)
            count = intValue;
        else if (value is double doubleValue)
            count = (int)doubleValue;
        else if (value is float floatValue)
            count = (int)floatValue;
        else if (value is decimal decimalValue)
            count = (int)decimalValue;
        else if (value is long longValue)
            count = (int)longValue;
        else if (value is string stringValue && int.TryParse(stringValue, out int parsedValue))
            count = parsedValue;

        if (parameter != null)
        {
            if (int.TryParse(parameter.ToString(), out int paramValue))
            {
                return count == paramValue ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        return count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

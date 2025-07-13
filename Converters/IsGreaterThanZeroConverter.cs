using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Değerin sıfırdan büyük olup olmadığını kontrol eden converter
/// </summary>
public class IsGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intValue)
            return intValue > 0;

        if (value is double doubleValue)
            return doubleValue > 0;

        if (value is float floatValue)
            return floatValue > 0;

        if (value is decimal decimalValue)
            return decimalValue > 0;

        if (value is long longValue)
            return longValue > 0;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for IsGreaterThanZeroConverter");
    }
}

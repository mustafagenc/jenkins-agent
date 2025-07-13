using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// String değerin null veya boş olup olmadığını kontrol eden converter
/// </summary>
public class IsNotNullOrEmptyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !string.IsNullOrEmpty(value?.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for IsNotNullOrEmptyConverter");
    }
}

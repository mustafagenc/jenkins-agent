using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Nesnenin null olup olmadığını kontrol eden converter
/// </summary>
public class IsNotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for IsNotNullConverter");
    }
}

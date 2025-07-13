using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Boolean değeri favorite icon'a çeviren converter
/// </summary>
public class BoolToFavoriteIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isFavorite && isFavorite)
            return "★"; // Filled star

        return "☆"; // Empty star
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for BoolToFavoriteIconConverter");
    }
}

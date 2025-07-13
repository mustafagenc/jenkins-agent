using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

public class BoolToConnectionIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? "✓" : "✗";
        }
        return "✗";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

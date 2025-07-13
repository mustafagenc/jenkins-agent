using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JenkinsAgent.Converters;

/// <summary>
/// Converts building status to button text (Play/Stop)
/// </summary>
public class BuildingToButtonTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBuilding)
        {
            return isBuilding ? "⏹" : "▶";
        }
        return "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts building status to button color
/// </summary>
public class BuildingToButtonColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBuilding)
        {
            return new SolidColorBrush(isBuilding ? Color.FromRgb(244, 67, 54) : Color.FromRgb(76, 175, 80));
        }
        return new SolidColorBrush(Color.FromRgb(76, 175, 80));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts building status to button tooltip
/// </summary>
public class BuildingToButtonTooltipConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isBuilding)
        {
            return isBuilding ? "Build'i Durdur" : "Job'ı Çalıştır";
        }
        return "Job'ı Çalıştır";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

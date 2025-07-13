using JenkinsAgent.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu other (disabled, not built etc.) visibility'ye çeviren converter
/// </summary>
public class JobStatusToOtherVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return (status == JobStatus.Disabled || status == JobStatus.NotBuilt || status == JobStatus.Unknown)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        var stringValue = value?.ToString()?.ToLower();
        if (stringValue == "disabled" || stringValue == "notbuilt" || stringValue == "grey" || stringValue == "unknown")
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for JobStatusToOtherVisibilityConverter");
    }
}

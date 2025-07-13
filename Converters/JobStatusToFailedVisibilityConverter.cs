using JenkinsAgent.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu failed visibility'ye çeviren converter
/// </summary>
public class JobStatusToFailedVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return (status == JobStatus.Failed || status == JobStatus.Unstable) ? Visibility.Visible : Visibility.Collapsed;
        }

        var stringValue = value?.ToString()?.ToLower();
        if (stringValue == "failed" || stringValue == "failure" || stringValue == "unstable")
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for JobStatusToFailedVisibilityConverter");
    }
}

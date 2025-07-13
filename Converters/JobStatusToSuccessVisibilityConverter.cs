using JenkinsAgent.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu success visibility'ye çeviren converter
/// </summary>
public class JobStatusToSuccessVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status == JobStatus.Success ? Visibility.Visible : Visibility.Collapsed;
        }

        if (value?.ToString()?.ToLower() == "success" || value?.ToString()?.ToLower() == "successful")
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for JobStatusToSuccessVisibilityConverter");
    }
}

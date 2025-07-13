using JenkinsAgent.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu renk koduna çeviren converter
/// </summary>
public class JobStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            var colorString = status switch
            {
                JobStatus.Success => "#4CAF50",
                JobStatus.Failed => "#F44336",
                JobStatus.Unstable => "#FF9800",
                JobStatus.Building => "#2196F3",
                JobStatus.Disabled => "#9E9E9E",
                JobStatus.NotBuilt => "#607D8B",
                JobStatus.Unknown => "#795548",
                _ => "#9E9E9E"
            };

            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
        }

        return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for JobStatusToColorConverter");
    }
}

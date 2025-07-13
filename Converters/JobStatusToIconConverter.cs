using JenkinsAgent.Models;
using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu ikona çeviren converter
/// </summary>
public class JobStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Success => "✓",
                JobStatus.Failed => "✗",
                JobStatus.Unstable => "⚠",
                JobStatus.Building => "⚡",
                JobStatus.Disabled => "⊘",
                JobStatus.NotBuilt => "◯",
                JobStatus.Unknown => "?",
                _ => "?"
            };
        }

        return "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

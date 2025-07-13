using JenkinsAgent.Models;
using System.Globalization;
using System.Windows.Data;

namespace JenkinsAgent.Converters;

/// <summary>
/// Job status'unu Türkçe text'e çeviren converter
/// </summary>
public class JobStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Success => "Başarılı",
                JobStatus.Failed => "Başarısız",
                JobStatus.Unstable => "Kararsız",
                JobStatus.Building => "Çalışıyor",
                JobStatus.Disabled => "Devre Dışı",
                JobStatus.NotBuilt => "Hiç Çalışmadı",
                JobStatus.Unknown => "Bilinmiyor",
                _ => "Bilinmiyor"
            };
        }

        return "Bilinmiyor";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("ConvertBack is not supported for JobStatusToTextConverter");
    }
}

using JenkinsAgent.ViewModels;
using System.Text.Json.Serialization;

namespace JenkinsAgent.Models;

/// <summary>
/// Jenkins build bilgilerini temsil eder
/// </summary>
public class JenkinsBuild : BaseViewModel
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    private bool _building;
    [JsonPropertyName("building")]
    public bool Building
    {
        get => _building;
        set
        {
            if (SetProperty(ref _building, value))
            {
                // Building durumu değiştiğinde DurationText'i güncelle
                OnPropertyChanged(nameof(DurationText));
                OnPropertyChanged(nameof(RunningDuration));
            }
        }
    }

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("estimatedDuration")]
    public long EstimatedDuration { get; set; }

    [JsonPropertyName("executor")]
    public object? Executor { get; set; }

    /// <summary>
    /// Build zamanını DateTime olarak döndürür
    /// </summary>
    public DateTime BuildTime => DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).DateTime;

    /// <summary>
    /// Build süresini TimeSpan olarak döndürür
    /// </summary>
    public TimeSpan BuildDuration => TimeSpan.FromMilliseconds(Duration);

    /// <summary>
    /// Build durumunu enum olarak döndürür
    /// </summary>
    public BuildResult BuildResult => Result?.ToUpper() switch
    {
        "SUCCESS" => BuildResult.Success,
        "FAILURE" => BuildResult.Failure,
        "UNSTABLE" => BuildResult.Unstable,
        "ABORTED" => BuildResult.Aborted,
        "NOT_BUILT" => BuildResult.NotBuilt,
        null when Building => BuildResult.Building,
        _ => BuildResult.Unknown
    };

    /// <summary>
    /// Build'in devam ettiği süre (building ise)
    /// </summary>
    public TimeSpan? RunningDuration => Building ? DateTime.UtcNow - BuildTime : null;

    /// <summary>
    /// Build süresini kullanıcı dostu formatta döndürür
    /// </summary>
    public string DurationText
    {
        get
        {
            if (Building && RunningDuration.HasValue)
            {
                var duration = RunningDuration.Value;
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}sa {duration.Minutes}dk {duration.Seconds}sn (devam ediyor)";
                else if (duration.TotalMinutes >= 1)
                    return $"{duration.Minutes}dk {duration.Seconds}sn (devam ediyor)";
                else
                    return $"{duration.Seconds}sn (devam ediyor)";
            }
            else if (Duration > 0)
            {
                var duration = BuildDuration;
                if (duration.TotalHours >= 1)
                    return $"{(int)duration.TotalHours}sa {duration.Minutes}dk {duration.Seconds}sn";
                else if (duration.TotalMinutes >= 1)
                    return $"{duration.Minutes}dk {duration.Seconds}sn";
                else
                    return $"{duration.Seconds}sn";
            }
            else
            {
                return "Bilinmiyor";
            }
        }
    }
}

/// <summary>
/// Build sonuçları
/// </summary>
public enum BuildResult
{
    Success,
    Failure,
    Unstable,
    Aborted,
    NotBuilt,
    Building,
    Unknown
}

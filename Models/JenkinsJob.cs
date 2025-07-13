using JenkinsAgent.ViewModels;
using System.Text.Json.Serialization;

namespace JenkinsAgent.Models;

/// <summary>
/// Jenkins job bilgilerini temsil eder
/// </summary>
public class JenkinsJob : BaseViewModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    private string _color = string.Empty;
    [JsonPropertyName("color")]
    public string Color
    {
        get => _color;
        set
        {
            if (SetProperty(ref _color, value))
            {
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(IsCurrentlyBuilding));
                OnPropertyChanged(nameof(DebugStatus));
            }
        }
    }

    [JsonPropertyName("buildable")]
    public bool Buildable { get; set; }

    private bool _inQueue;
    [JsonPropertyName("inQueue")]
    public bool InQueue
    {
        get => _inQueue;
        set
        {
            if (SetProperty(ref _inQueue, value))
            {
                OnPropertyChanged(nameof(DebugStatus));
                OnPropertyChanged(nameof(IsCurrentlyBuilding));
            }
        }
    }

    [JsonPropertyName("_class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("builds")]
    public List<JenkinsBuild> Builds { get; set; } = new();

    private JenkinsBuild? _lastBuild;
    [JsonPropertyName("lastBuild")]
    public JenkinsBuild? LastBuild
    {
        get => _lastBuild;
        set
        {
            if (SetProperty(ref _lastBuild, value))
            {
                OnPropertyChanged(nameof(DebugStatus));
                OnPropertyChanged(nameof(IsCurrentlyBuilding));
            }
        }
    }

    [JsonPropertyName("lastSuccessfulBuild")]
    public JenkinsBuild? LastSuccessfulBuild { get; set; }

    [JsonPropertyName("lastFailedBuild")]
    public JenkinsBuild? LastFailedBuild { get; set; }

    private string? _description;
    [JsonPropertyName("description")]
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    [JsonPropertyName("jobs")]
    public List<JenkinsJob> Jobs { get; set; } = new();

    [JsonIgnore]
    public string StatusColor { get; set; } = "#3B82F6";

    [JsonIgnore]
    public string LastBuildStatus { get; set; } = "Henüz build edilmedi";

    [JsonIgnore]
    public string FullPath { get; set; } = string.Empty;

    /// <summary>
    /// Bu item'ın folder olup olmadığını kontrol eder
    /// </summary>
    [JsonIgnore]
    public bool IsFolder { get; set; }

    /// <summary>
    /// Bu item'ın gerçek bir job olup olmadığını kontrol eder
    /// </summary>
    [JsonIgnore]
    public bool IsJob => !IsFolder && Buildable;

    /// <summary>
    /// Automatic folder detection based on class
    /// </summary>
    [JsonIgnore]
    public bool IsAutomaticFolder => Class.Contains("Folder") || Class.Contains("WorkflowMultiBranchProject") || Jobs.Any();

    /// <summary>
    /// Job durumunu renk kodundan çıkarır
    /// </summary>
    public JobStatus Status => Color switch
    {
        "blue" => JobStatus.Success,
        "red" => JobStatus.Failed,
        "yellow" => JobStatus.Unstable,
        "grey" => JobStatus.Disabled,
        "notbuilt" => JobStatus.NotBuilt,
        var c when c.EndsWith("_anime") => JobStatus.Building,
        _ => JobStatus.Unknown
    };

    /// <summary>
    /// Job'ın şu anda gerçekten çalışıp çalışmadığını kontrol eder
    /// </summary>
    [JsonIgnore]
    public bool IsCurrentlyBuilding => LastBuild?.Building == true || InQueue || Status == JobStatus.Building;

    /// <summary>
    /// Debug için - UI'da job'ın mevcut durumunu gösterir
    /// </summary>
    [JsonIgnore]
    public string DebugStatus => $"Building: {LastBuild?.Building ?? false}, Status: {Status}, InQueue: {InQueue}";

    /// <summary>
    /// Job'ın favori olup olmadığı (local ayar)
    /// </summary>
    private bool _isFavorite;
    public bool IsFavorite
    {
        get => _isFavorite;
        set => SetProperty(ref _isFavorite, value);
    }

    /// <summary>
    /// Job'ın kuyruğa giriş zamanı
    /// </summary>
    [JsonIgnore]
    public DateTime QueueTime { get; set; } = DateTime.Now;
}

/// <summary>
/// Job durumları
/// </summary>
public enum JobStatus
{
    Success,
    Failed,
    Unstable,
    Disabled,
    NotBuilt,
    Building,
    Unknown
}

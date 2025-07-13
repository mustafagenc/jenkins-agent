using System.Text.Json.Serialization;

namespace JenkinsAgent.Models;

/// <summary>
/// Jenkins sunucu bilgilerini temsil eder
/// </summary>
public class JenkinsInfo
{
    [JsonPropertyName("assignedLabels")]
    public List<object> AssignedLabels { get; set; } = new();

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("nodeDescription")]
    public string NodeDescription { get; set; } = string.Empty;

    [JsonPropertyName("nodeName")]
    public string NodeName { get; set; } = string.Empty;

    [JsonPropertyName("numExecutors")]
    public int NumExecutors { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("jobs")]
    public List<JenkinsJob> Jobs { get; set; } = new();

    [JsonPropertyName("views")]
    public List<JenkinsView> Views { get; set; } = new();

    [JsonPropertyName("primaryView")]
    public JenkinsView? PrimaryView { get; set; }

    [JsonPropertyName("quietingDown")]
    public bool QuietingDown { get; set; }

    [JsonPropertyName("slaveAgentPort")]
    public int SlaveAgentPort { get; set; }

    [JsonPropertyName("unlabeledLoad")]
    public object? UnlabeledLoad { get; set; }

    [JsonPropertyName("useCrumbs")]
    public bool UseCrumbs { get; set; }

    [JsonPropertyName("useSecurity")]
    public bool UseSecurity { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Jenkins view bilgilerini temsil eder
/// </summary>
public class JenkinsView
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Jenkins queue item bilgilerini temsil eder
/// </summary>
public class JenkinsQueueItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("inQueueSince")]
    public long InQueueSince { get; set; }

    [JsonPropertyName("params")]
    public string Params { get; set; } = string.Empty;

    [JsonPropertyName("stuck")]
    public bool Stuck { get; set; }

    [JsonPropertyName("task")]
    public JenkinsTask Task { get; set; } = new();

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("why")]
    public string? Why { get; set; }

    /// <summary>
    /// Kuyrukta bekleme zamanı
    /// </summary>
    public DateTime QueueTime => DateTimeOffset.FromUnixTimeMilliseconds(InQueueSince).DateTime;

    /// <summary>
    /// Kuyrukta bekleme süresi
    /// </summary>
    public TimeSpan WaitingTime => DateTime.UtcNow - QueueTime;
}

/// <summary>
/// Jenkins task bilgilerini temsil eder
/// </summary>
public class JenkinsTask
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// Jenkins executor bilgilerini temsil eder
/// </summary>
public class JenkinsExecutor
{
    [JsonPropertyName("idle")]
    public bool Idle { get; set; }

    [JsonPropertyName("likelyStuck")]
    public bool LikelyStuck { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("currentExecutable")]
    public JenkinsExecutable? CurrentExecutable { get; set; }
}

/// <summary>
/// Jenkins executable bilgilerini temsil eder
/// </summary>
public class JenkinsExecutable
{
    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("fullDisplayName")]
    public string FullDisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Computer executor bilgilerini temsil eder
/// </summary>
public class ComputerExecutor
{
    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("executors")]
    public List<JenkinsExecutor> Executors { get; set; } = new();

    [JsonPropertyName("idle")]
    public bool Idle { get; set; }

    [JsonPropertyName("offline")]
    public bool Offline { get; set; }
}

/// <summary>
/// Computer list response
/// </summary>
public class ComputerResponse
{
    [JsonPropertyName("computer")]
    public List<ComputerExecutor> Computer { get; set; } = new();
}

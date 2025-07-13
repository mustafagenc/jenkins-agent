namespace JenkinsAgent.Models;

/// <summary>
/// Uygulama ayarları için model
/// </summary>
public class AppSettings
{
    public JenkinsConfig Jenkins { get; set; } = new();
    public List<string> FavoriteJobs { get; set; } = new();
    public List<string> ProjectFolders { get; set; } = new();
    public string LastSelectedFolder { get; set; } = "";
    public string LastSelectedFolderFullPath { get; set; } = "";
    public bool MinimizeToTray { get; set; } = true;
    public bool StartWithWindows { get; set; } = false;
    public int MonitoringIntervalSeconds { get; set; } = 5;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
}

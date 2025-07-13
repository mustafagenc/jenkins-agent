using JenkinsAgent.Models;

namespace JenkinsAgent.Services;

/// <summary>
/// Jenkins API servisi interface'i
/// </summary>
public interface IJenkinsApiService
{
    /// <summary>
    /// Jenkins sunucu bilgilerini alır
    /// </summary>
    Task<JenkinsInfo?> GetJenkinsInfoAsync();

    /// <summary>
    /// Tüm job'ları alır
    /// </summary>
    Task<List<JenkinsJob>> GetJobsAsync();

    /// <summary>
    /// Belirli bir job'ın detaylarını alır
    /// </summary>
    Task<JenkinsJob?> GetJobAsync(string jobName);

    /// <summary>
    /// Job'ı çalıştırır (parametresiz)
    /// </summary>
    Task<bool> BuildJobAsync(string jobName);

    /// <summary>
    /// Çalışan job'ı durdurur
    /// </summary>
    Task<bool> StopJobAsync(string jobName, int buildNumber);

    /// <summary>
    /// Folder içindeki çalışan job'ı durdurur
    /// </summary>
    Task<bool> StopJobInFolderAsync(string folderName, string jobName, int buildNumber);

    /// <summary>
    /// Folder içindeki job'ı çalıştırır
    /// </summary>
    Task<bool> BuildJobInFolderAsync(string folderName, string jobName);

    /// <summary>
    /// Folder içindeki job'ı çalıştırır (alternatif yöntemler deneyerek)
    /// </summary>
    Task<bool> BuildJobInFolderAlternativeAsync(string folderName, string jobName);

    /// <summary>
    /// Job'ı parametrelerle çalıştırır
    /// </summary>
    Task<bool> BuildJobWithParametersAsync(string jobName, Dictionary<string, string> parameters);

    /// <summary>
    /// Belirli bir build'in detaylarını alır
    /// </summary>
    Task<JenkinsBuild?> GetBuildAsync(string jobName, int buildNumber);

    /// <summary>
    /// Build loglarını alır
    /// </summary>
    Task<string> GetBuildLogsAsync(string jobName, int buildNumber);

    /// <summary>
    /// Son build'i alır
    /// </summary>
    Task<JenkinsBuild?> GetLastBuildAsync(string jobName);

    /// <summary>
    /// Build kuyruğunu alır
    /// </summary>
    Task<List<JenkinsQueueItem>> GetQueueAsync();

    /// <summary>
    /// Jenkins executor bilgilerini alır
    /// </summary>
    Task<List<ComputerExecutor>> GetExecutorsAsync();

    /// <summary>
    /// Folder'ları (klasörleri) alır
    /// </summary>
    Task<List<JenkinsJob>> GetFoldersAsync();

    /// <summary>
    /// Belirli bir folder'ın içindeki job'ları alır
    /// </summary>
    Task<List<JenkinsJob>> GetJobsInFolderAsync(string folderPath);

    /// <summary>
    /// Jenkins sunucusuna bağlantıyı test eder
    /// </summary>
    Task<bool> TestConnectionAsync();

    /// <summary>
    /// Konfigürasyonu günceller
    /// </summary>
    void UpdateConfiguration(JenkinsConfig config);
}

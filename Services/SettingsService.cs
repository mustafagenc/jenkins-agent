using JenkinsAgent.Models;

namespace JenkinsAgent.Services;

/// <summary>
/// Settings yönetimi için service
/// </summary>
public class SettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;
    private readonly SemaphoreSlim _settingsSemaphore = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "JenkinsAgent");
        Directory.CreateDirectory(appFolder);
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        await _settingsSemaphore.WaitAsync();
        try
        {
            // Emin olmak için iki kez kontrol ediyorum
            if (_cachedSettings != null)
                return _cachedSettings;

            if (File.Exists(_settingsFilePath))
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
                await SaveSettingsInternalAsync(_cachedSettings);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings load error: {ex.Message}");
            JenkinsAgent.ViewModels.ErrorLogger.Log(ex, "SettingsService.LoadSettingsAsync");
            _cachedSettings = new AppSettings();
        }
        finally
        {
            _settingsSemaphore.Release();
        }

        return _cachedSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        await _settingsSemaphore.WaitAsync();
        try
        {
            await SaveSettingsInternalAsync(settings);
        }
        finally
        {
            _settingsSemaphore.Release();
        }
    }

    private async Task SaveSettingsInternalAsync(AppSettings settings)
    {
        try
        {
            settings.LastUpdated = DateTime.Now;

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _cachedSettings = settings;

            System.Diagnostics.Debug.WriteLine($"Settings saved to: {_settingsFilePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Settings save error: {ex.Message}");
            JenkinsAgent.ViewModels.ErrorLogger.Log(ex, "SettingsService.SaveSettingsInternalAsync");
            throw; // Hatanın üst katmana iletilmesini istiyorum
        }
    }

    public async Task AddFavoriteJobAsync(string jobName)
    {
        var settings = await LoadSettingsAsync();
        if (!settings.FavoriteJobs.Contains(jobName))
        {
            settings.FavoriteJobs.Add(jobName);
            await SaveSettingsAsync(settings);
        }
    }

    public async Task RemoveFavoriteJobAsync(string jobName)
    {
        var settings = await LoadSettingsAsync();
        if (settings.FavoriteJobs.Remove(jobName))
        {
            await SaveSettingsAsync(settings);
        }
    }

    public async Task AddProjectFolderAsync(string folderName)
    {
        var settings = await LoadSettingsAsync();
        if (!settings.ProjectFolders.Contains(folderName))
        {
            settings.ProjectFolders.Add(folderName);
            await SaveSettingsAsync(settings);
        }
    }

    public async Task RemoveProjectFolderAsync(string folderName)
    {
        var settings = await LoadSettingsAsync();
        if (settings.ProjectFolders.Remove(folderName))
        {
            await SaveSettingsAsync(settings);
        }
    }

    public string GetSettingsFilePath() => _settingsFilePath;
}

using JenkinsAgent.Commands;
using JenkinsAgent.Models;
using JenkinsAgent.Services;
using System.Windows.Threading;

namespace JenkinsAgent.ViewModels;

public class MainWindowViewModel : BaseViewModel
{
    private readonly IJenkinsApiService _jenkinsApiService;
    private readonly SettingsService _settingsService;
    private readonly DispatcherTimer _jobMonitorTimer;

    private bool _isConnected;
    private bool _isLoading;
    private string _statusMessage = "Bağlantı bekleniyor...";
    private JenkinsConfig _jenkinsConfig = new();
    private bool _isMonitoring = false;
    private bool _showSettingsWarning = false;
    private bool _showJobsSection = true;
    private string _searchText = string.Empty;

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    public bool ShowSettingsWarning
    {
        get => _showSettingsWarning;
        set => SetProperty(ref _showSettingsWarning, value);
    }

    public bool ShowJobsSection
    {
        get => _showJobsSection;
        set => SetProperty(ref _showJobsSection, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public JenkinsConfig JenkinsConfig
    {
        get => _jenkinsConfig;
        set => SetProperty(ref _jenkinsConfig, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterJobs();
            }
        }
    }



    public int FilteredJobsCount => FilteredJobs?.Count ?? 0;

    private ObservableCollection<JenkinsJob> _filteredJobs = new();
    public ObservableCollection<JenkinsJob> FilteredJobs
    {
        get => _filteredJobs;
        set => SetProperty(ref _filteredJobs, value);
    }

    public ObservableCollection<JenkinsJob> Jobs { get; } = new();
    public ObservableCollection<JenkinsJob> Folders { get; } = new();
    public ObservableCollection<JenkinsJob> FavoriteJobs { get; } = new();
    public ObservableCollection<JenkinsJob> QueuedBuilds { get; } = new();
    public ObservableCollection<JenkinsJob> RunningBuilds { get; } = new();

    private int _totalExecutors;
    private int _busyExecutors;

    public int TotalExecutors
    {
        get => _totalExecutors;
        set => SetProperty(ref _totalExecutors, value);
    }

    public int BusyExecutors
    {
        get => _busyExecutors;
        set => SetProperty(ref _busyExecutors, value);
    }

    public int IdleExecutors => TotalExecutors - BusyExecutors;

    public string ExecutorSummary => TotalExecutors > 0 ? $"{BusyExecutors}/{TotalExecutors} executor meşgul" : "Executor bilgisi yok";

    private JenkinsJob? _selectedFolder;
    public JenkinsJob? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
            {
                _ = LoadJobsInSelectedFolderAsync();
            }
        }
    }

    public ICommand ConnectCommand { get; }
    public ICommand RefreshJobsCommand { get; }
    public ICommand RefreshFoldersCommand { get; }
    public ICommand SelectFolderCommand { get; }
    public ICommand BuildJobCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand ShowWindowCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand StartMonitoringCommand { get; }
    public ICommand StopMonitoringCommand { get; }
    public ICommand OpenBlueOceanCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAboutCommand { get; }
    public ICommand ConfigureJobCommand { get; }
    public ICommand OpenJenkinsHomeCommand { get; }


    public MainWindowViewModel(IJenkinsApiService jenkinsApiService, SettingsService settingsService)
    {
        _jenkinsApiService = jenkinsApiService;
        _settingsService = settingsService;

        ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => !IsLoading);
        RefreshJobsCommand = new RelayCommand(async () => await LoadJobsInSelectedFolderAsync(), () => IsConnected && !IsLoading && SelectedFolder != null);
        RefreshFoldersCommand = new RelayCommand(async () => await RefreshFoldersAsync(), () => IsConnected && !IsLoading);
        SelectFolderCommand = new RelayCommand<JenkinsJob>(folder => SelectedFolder = folder, folder => folder != null);
        BuildJobCommand = new RelayCommand<JenkinsJob>(async (job) => await BuildJobAsync(job), (job) => job != null && IsConnected);
        ToggleFavoriteCommand = new RelayCommand<JenkinsJob>(ToggleFavorite, (job) => job != null);
        SaveSettingsCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => !IsLoading);
        ShowWindowCommand = new RelayCommand(ShowWindow);
        ExitCommand = new RelayCommand(ExitApplication);
        StartMonitoringCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => IsConnected && !IsMonitoring);
        StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
        OpenBlueOceanCommand = new RelayCommand<JenkinsJob>(async (job) => await OpenBlueOceanAsync(job), (job) => job != null && IsConnected);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        OpenAboutCommand = new RelayCommand(OpenAbout);
        ConfigureJobCommand = new RelayCommand<JenkinsJob>(async (job) => await ConfigureJobAsync(job), (job) => job != null && IsConnected);
        OpenJenkinsHomeCommand = new RelayCommand(async () => await OpenJenkinsHomeAsync(), () => IsConnected);


        _jobMonitorTimer = new DispatcherTimer();
        _jobMonitorTimer.Tick += async (s, e) => await MonitorJobsAsync();
        LoadSettings();
    }

    private async Task ConnectAsync()
    {
        IsLoading = true;
        StatusMessage = "Bağlanıyor...";

        try
        {
            if (string.IsNullOrWhiteSpace(JenkinsConfig.BaseUrl))
            {
                StatusMessage = "Lütfen Jenkins sunucu URL'sini girin";
                return;
            }

            if (string.IsNullOrWhiteSpace(JenkinsConfig.Username))
            {
                StatusMessage = "Lütfen kullanıcı adını girin";
                return;
            }

            if (string.IsNullOrWhiteSpace(JenkinsConfig.ApiToken))
            {
                StatusMessage = "Lütfen API token'ını girin";
                return;
            }

            if (!JenkinsConfig.BaseUrl.StartsWith("http://") && !JenkinsConfig.BaseUrl.StartsWith("https://"))
            {
                JenkinsConfig.BaseUrl = "http://" + JenkinsConfig.BaseUrl;
            }

            StatusMessage = $"Bağlanıyor: {JenkinsConfig.BaseUrl}";

            _jenkinsApiService.UpdateConfiguration(JenkinsConfig);
            var connectionResult = await _jenkinsApiService.TestConnectionAsync();

            if (connectionResult)
            {
                IsConnected = true;
                StatusMessage = "Bağlantı başarılı - Klasörler yükleniyor...";
                await RefreshFoldersAsync();
            }
            else
            {
                IsConnected = false;
                StatusMessage = $"Bağlantı başarısız - {JenkinsConfig.BaseUrl} adresine erişilemiyor. URL, kullanıcı adı ve API token'ınızı kontrol edin.";
            }
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Bağlantı hatası: {ex.Message}";
            ErrorLogger.Log(ex, "ConnectAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshJobsAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        StatusMessage = "Job'lar yükleniyor...";

        try
        {
            var jobs = await _jenkinsApiService.GetJobsAsync();
            var settings = await _settingsService.LoadSettingsAsync();

            foreach (var newJob in jobs)
            {
                var existingJob = Jobs.FirstOrDefault(j => j.Name == newJob.Name && j.FullPath == newJob.FullPath);

                if (existingJob != null)
                {
                    existingJob.Description = newJob.Description;
                    existingJob.LastBuild = newJob.LastBuild;
                    existingJob.InQueue = newJob.InQueue;
                    existingJob.IsFavorite = settings.FavoriteJobs.Contains(newJob.Name);

                    existingJob.OnPropertyChanged(nameof(existingJob.IsCurrentlyBuilding));
                    existingJob.OnPropertyChanged(nameof(existingJob.DebugStatus));
                }
                else
                {
                    newJob.IsFavorite = settings.FavoriteJobs.Contains(newJob.Name);

                    newJob.OnPropertyChanged(nameof(newJob.IsCurrentlyBuilding));
                    newJob.OnPropertyChanged(nameof(newJob.DebugStatus));

                    Jobs.Add(newJob);
                }
            }

            var jobsToRemove = Jobs.Where(j => !jobs.Any(nj => nj.Name == j.Name && nj.FullPath == j.FullPath)).ToList();
            foreach (var jobToRemove in jobsToRemove)
            {
                Jobs.Remove(jobToRemove);
                FavoriteJobs.Remove(jobToRemove);
            }

            FavoriteJobs.Clear();
            foreach (var job in Jobs.Where(j => j.IsFavorite))
            {
                FavoriteJobs.Add(job);
            }

            FilterJobs();
            StatusMessage = $"{jobs.Count} job yüklendi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Job'lar yüklenirken hata: {ex.Message}";
            ErrorLogger.Log(ex, "RefreshJobsAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static void LogDebug(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        try
        {
            var logFile = Path.Combine(Path.GetTempPath(), "jenkins-agent-debug.log");
            File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - ViewModel - {message}\n");
        }
        catch
        {
        }
    }

    private async Task BuildJobAsync(JenkinsJob? job)
    {
        if (job == null || !IsConnected) return;

        try
        {
            if (job.IsCurrentlyBuilding)
            {
                StatusMessage = $"{job.Name} job'ı durduruluyor...";
                LogDebug($"BuildJobAsync - Attempting to stop running job: '{job.Name}'");

                var stopResult = await StopJobAsync(job);
                if (stopResult)
                {
                    StatusMessage = $"{job.Name} job'ı durduruldu";
                }
                else
                {
                    StatusMessage = $"{job.Name} job'ı durdurulamadı";
                }
                return;
            }

            StatusMessage = $"{job.Name} job'ı başlatılıyor...";
            LogDebug($"BuildJobAsync - Job Name: '{job.Name}', Job FullPath: '{job.FullPath}', SelectedFolder: '{SelectedFolder?.Name}'");

            bool result;

            if (SelectedFolder != null && !string.IsNullOrEmpty(SelectedFolder.Name) && SelectedFolder.Name != "Tüm Job'lar")
            {
                LogDebug($"BuildJobAsync - Using BuildJobInFolderAsync with folder: '{SelectedFolder.Name}' and job: '{job.Name}'");
                result = await _jenkinsApiService.BuildJobInFolderAsync(SelectedFolder.Name, job.Name);

                if (!result)
                {
                    LogDebug($"BuildJobAsync - First attempt failed, trying alternative method");
                    result = await _jenkinsApiService.BuildJobInFolderAlternativeAsync(SelectedFolder.Name, job.Name);
                }
            }
            else
            {
                var jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                LogDebug($"BuildJobAsync - Using BuildJobAsync with job name: '{jobName}'");
                result = await _jenkinsApiService.BuildJobAsync(jobName);
            }

            LogDebug($"BuildJobAsync - Build result: {result}");

            if (result)
            {
                StatusMessage = $"{job.Name} job'ı başarıyla başlatıldı";
                LogDebug($"BuildJobAsync - Job build successful for: {job.Name}");

                if (job.LastBuild != null)
                {
                    job.LastBuild.Building = true;
                    if (job.LastBuild.Timestamp == 0 || job.LastBuild.BuildTime < DateTime.UtcNow.AddMinutes(-1))
                    {
                        job.LastBuild.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    }
                    LogDebug($"BuildJobAsync - Set existing LastBuild.Building = true for job: {job.Name}, timestamp: {job.LastBuild.Timestamp}");
                }
                else
                {
                    var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    job.LastBuild = new JenkinsBuild
                    {
                        Building = true,
                        Number = 0,
                        Timestamp = currentTimestamp,
                        DisplayName = "Başlatılıyor..."
                    };
                    LogDebug($"BuildJobAsync - Created temporary LastBuild for job: {job.Name} with timestamp: {currentTimestamp}");
                }

                job.OnPropertyChanged(nameof(job.IsCurrentlyBuilding));
                job.OnPropertyChanged(nameof(job.LastBuild));
                job.OnPropertyChanged(nameof(job.DebugStatus));

                LogDebug($"BuildJobAsync - Job '{job.Name}' IsCurrentlyBuilding: {job.IsCurrentlyBuilding}, LastBuild.Building: {job.LastBuild?.Building}, InQueue: {job.InQueue}");

                await StartMonitoringAsync();

                StatusMessage = $"{job.Name} job'ı çalışıyor - İzleme aktif";
            }
            else
            {
                StatusMessage = $"{job.Name} job'ı başlatılamadı - API çağrısı başarısız";
                LogDebug($"BuildJobAsync - Failed to build job: {job.Name}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Job işlemi sırasında hata: {ex.Message}";
            ErrorLogger.Log(ex, "BuildJobAsync");
        }
    }

    private async Task<bool> StopJobAsync(JenkinsJob job)
    {
        try
        {
            LogDebug($"StopJobAsync - Attempting to stop job: {job.Name}");
            LogDebug($"StopJobAsync - Job.FullPath: '{job.FullPath}', SelectedFolder: '{SelectedFolder?.Name}'");
            LogDebug($"StopJobAsync - LastBuild: {(job.LastBuild != null ? $"Number={job.LastBuild.Number}, Building={job.LastBuild.Building}" : "null")}");

            if (job.LastBuild != null && job.LastBuild.Building)
            {
                string jobName;
                bool result = false;

                int buildNumber = job.LastBuild.Number;
                if (buildNumber <= 0)
                {
                    LogDebug($"StopJobAsync - Build number is {buildNumber}, trying to get actual running build number");
                    try
                    {
                        var jobNameForApi = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                        var latestBuild = await _jenkinsApiService.GetLastBuildAsync(jobNameForApi);
                        if (latestBuild != null && latestBuild.Building && latestBuild.Number > 0)
                        {
                            buildNumber = latestBuild.Number;
                            LogDebug($"StopJobAsync - Found actual running build number: {buildNumber}");
                        }
                        else
                        {
                            LogDebug($"StopJobAsync - Could not find valid running build number");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"StopJobAsync - Error getting actual build number: {ex.Message}");
                        return false;
                    }
                }

                if (SelectedFolder != null && !string.IsNullOrEmpty(SelectedFolder.Name) && SelectedFolder.Name != "Tüm Job'lar")
                {
                    jobName = job.Name;
                    LogDebug($"StopJobAsync - Trying folder-based stop: folder='{SelectedFolder.Name}', job='{jobName}', build={buildNumber}");

                    try
                    {
                        result = await _jenkinsApiService.StopJobInFolderAsync(SelectedFolder.Name, jobName, buildNumber);
                        LogDebug($"StopJobAsync - Folder-based stop result: {result}");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"StopJobAsync - Folder-based stop failed: {ex.Message}");
                        result = false;
                    }

                    if (!result)
                    {
                        jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : $"{SelectedFolder.Name}/{job.Name}";
                        LogDebug($"StopJobAsync - Trying with FullPath: '{jobName}', build={buildNumber}");
                        result = await _jenkinsApiService.StopJobAsync(jobName, buildNumber);
                    }
                }
                else
                {
                    jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                    LogDebug($"StopJobAsync - Root level job: '{jobName}', build={buildNumber}");
                    result = await _jenkinsApiService.StopJobAsync(jobName, buildNumber);
                }

                LogDebug($"StopJobAsync - Final result: {result}");

                if (result)
                {
                    LogDebug($"StopJobAsync - Successfully stopped job: {job.Name}");

                    if (job.LastBuild != null)
                    {
                        job.LastBuild.Building = false;
                        job.OnPropertyChanged(nameof(job.IsCurrentlyBuilding));
                        job.OnPropertyChanged(nameof(job.LastBuild));
                        job.OnPropertyChanged(nameof(job.DebugStatus));
                    }

                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(1000); // 1 saniye bekle
                        await LoadJobsInSelectedFolderAsync();
                    });
                }
                else
                {
                    LogDebug($"StopJobAsync - Failed to stop job: {job.Name}");
                }

                return result;
            }
            else
            {
                LogDebug($"StopJobAsync - Job is not currently building or has no LastBuild");
                return false;
            }
        }
        catch (Exception ex)
        {
            LogDebug($"StopJobAsync exception: {ex.GetType().Name}: {ex.Message}");
            LogDebug($"StopJobAsync stack trace: {ex.StackTrace}");
            ErrorLogger.Log(ex, "StopJobAsync");
            return false;
        }
    }

    private void ToggleFavorite(JenkinsJob? job)
    {
        if (job == null) return;

        job.IsFavorite = !job.IsFavorite;

        if (job.IsFavorite)
        {
            if (!FavoriteJobs.Contains(job))
                FavoriteJobs.Add(job);
        }
        else
        {
            FavoriteJobs.Remove(job);
        }

        // Save to settings
        _ = Task.Run(async () =>
        {
            var settings = await _settingsService.LoadSettingsAsync();
            if (job.IsFavorite)
            {
                if (!settings.FavoriteJobs.Contains(job.Name))
                    settings.FavoriteJobs.Add(job.Name);
            }
            else
            {
                settings.FavoriteJobs.Remove(job.Name);
            }
            await _settingsService.SaveSettingsAsync(settings);
        });
    }

    private async void LoadSettings()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            JenkinsConfig = settings.Jenkins;
            LogDebug($"LoadSettings - Loaded Jenkins config. BaseUrl: '{settings.Jenkins.BaseUrl}', Username: '{settings.Jenkins.Username}'");
            LogDebug($"LoadSettings - LastSelectedFolder: '{settings.LastSelectedFolder}', LastSelectedFolderFullPath: '{settings.LastSelectedFolderFullPath}'");
            LogDebug($"LoadSettings - Settings file location: {_settingsService.GetSettingsFilePath()}");

            // İzleme aralığını ayarlıyorum (varsayılan: 3 sn)
            int intervalSec = settings.MonitoringIntervalSeconds > 0 ? settings.MonitoringIntervalSeconds : 3;
            _jobMonitorTimer.Interval = TimeSpan.FromSeconds(intervalSec);

            // Check if settings are configured and auto-connect
            await CheckAndAutoConnectAsync();
        }
        catch (Exception ex)
        {
            // Default settings will be used
            LogDebug($"LoadSettings - Error loading settings: {ex.Message}");
            LogDebug($"LoadSettings - Using default settings");
            ErrorLogger.Log(ex, "LoadSettings");

            // Show settings warning
            ShowSettingsWarning = true;
            ShowJobsSection = false;
        }
    }

    public async Task SaveSettingsAsync()
    {
        try
        {
            LogDebug($"SaveSettingsAsync - Saving settings. SelectedFolder: '{SelectedFolder?.Name}', FullPath: '{SelectedFolder?.FullPath}'");
            LogDebug($"SaveSettingsAsync - Jenkins URL: '{JenkinsConfig.BaseUrl}', Username: '{JenkinsConfig.Username}'");

            var settings = await _settingsService.LoadSettingsAsync();
            settings.Jenkins = JenkinsConfig;

            // Seçili klasör bilgisini kaydet
            if (SelectedFolder != null)
            {
                settings.LastSelectedFolder = SelectedFolder.Name;
                settings.LastSelectedFolderFullPath = SelectedFolder.FullPath ?? "";
                LogDebug($"SaveSettingsAsync - Saved selected folder: '{settings.LastSelectedFolder}' with path: '{settings.LastSelectedFolderFullPath}'");
            }

            await _settingsService.SaveSettingsAsync(settings);
            StatusMessage = "Ayarlar başarıyla kaydedildi";
            LogDebug("SaveSettingsAsync - Settings saved successfully");
            LogDebug($"SaveSettingsAsync - Settings file location: {_settingsService.GetSettingsFilePath()}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar kaydedilirken hata: {ex.Message}";
            LogDebug($"SaveSettingsAsync - Error saving settings: {ex.Message}");
            LogDebug($"SaveSettingsAsync - Error stack trace: {ex.StackTrace}");
            ErrorLogger.Log(ex, "SaveSettingsAsync");
        }
    }

    private async Task RefreshFoldersAsync()
    {
        if (!IsConnected) return;

        IsLoading = true;
        StatusMessage = "Klasörler yükleniyor...";

        try
        {
            var folders = await _jenkinsApiService.GetFoldersAsync();

            Folders.Clear();

            // "Tüm Job'lar" seçeneği ekle
            var allJobsFolder = new JenkinsJob
            {
                Name = "Tüm Job'lar",
                FullPath = "",
                IsFolder = true
            };
            Folders.Add(allJobsFolder);

            foreach (var folder in folders)
            {
                Folders.Add(folder);
            }

            StatusMessage = $"{folders.Count} klasör yüklendi";
            LogDebug($"RefreshFoldersAsync - Loaded {folders.Count} folders");

            // Kaydedilen klasörü seç, yoksa ilk klasörü seç
            await SelectSavedOrFirstFolder();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Klasörler yüklenirken hata: {ex.Message}";
            LogDebug($"RefreshFoldersAsync - Error: {ex.Message}");
            ErrorLogger.Log(ex, "RefreshFoldersAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task SelectSavedOrFirstFolder()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            JenkinsJob? folderToSelect = null;

            // Eğer kaydedilen klasör varsa onu bul
            if (!string.IsNullOrEmpty(settings.LastSelectedFolder))
            {
                folderToSelect = Folders.FirstOrDefault(f =>
                    f.Name == settings.LastSelectedFolder &&
                    f.FullPath == settings.LastSelectedFolderFullPath);

                LogDebug($"SelectSavedOrFirstFolder - Looking for saved folder: '{settings.LastSelectedFolder}' with path: '{settings.LastSelectedFolderFullPath}'");

                if (folderToSelect != null)
                {
                    LogDebug($"SelectSavedOrFirstFolder - Found saved folder: '{folderToSelect.Name}'");
                }
                else
                {
                    LogDebug("SelectSavedOrFirstFolder - Saved folder not found, using first folder");
                }
            }

            // Kaydedilen klasör bulunamazsa ilk klasörü seç
            if (folderToSelect == null && Folders.Any())
            {
                folderToSelect = Folders.First();
                LogDebug($"SelectSavedOrFirstFolder - Using first folder: '{folderToSelect.Name}'");
            }

            if (folderToSelect != null)
            {
                SelectedFolder = folderToSelect;
                LogDebug($"SelectSavedOrFirstFolder - Selected folder: '{SelectedFolder.Name}'");
            }
        }
        catch (Exception ex)
        {
            LogDebug($"SelectSavedOrFirstFolder - Error: {ex.Message}");
            ErrorLogger.Log(ex, "SelectSavedOrFirstFolder");
            // Hata durumunda ilk klasörü seç
            if (Folders.Any())
            {
                SelectedFolder = Folders.First();
            }
        }
    }

    private async Task LoadJobsInSelectedFolderAsync()
    {
        if (!IsConnected || SelectedFolder == null) return;

        IsLoading = true;
        StatusMessage = $"{SelectedFolder.Name} klasöründeki job'lar yükleniyor...";

        try
        {
            List<JenkinsJob> jobs;

            if (string.IsNullOrEmpty(SelectedFolder.FullPath))
            {
                jobs = await _jenkinsApiService.GetJobsAsync();
            }
            else
            {
                jobs = await _jenkinsApiService.GetJobsInFolderAsync(SelectedFolder.FullPath);
            }

            Jobs.Clear();
            FavoriteJobs.Clear();

            var settings = await _settingsService.LoadSettingsAsync();

            foreach (var job in jobs)
            {
                job.IsFavorite = settings.FavoriteJobs.Contains(job.FullPath);


                Jobs.Add(job);

                if (job.IsFavorite)
                    FavoriteJobs.Add(job);
            }

            FilterJobs();

            await Task.Delay(100);
            foreach (var job in Jobs)
            {
                job.OnPropertyChanged(nameof(job.IsCurrentlyBuilding));
                job.OnPropertyChanged(nameof(job.LastBuild));
                job.OnPropertyChanged(nameof(job.DebugStatus));
            }
            StatusMessage = $"{jobs.Count} job yüklendi ({SelectedFolder.Name})";

            LogDebug($"LoadJobsInSelectedFolderAsync - Loaded {jobs.Count} jobs, FilteredJobs count: {FilteredJobs.Count}");

            await UpdateQueueAndExecutorsAsync();
            await UpdateQueueAndExecutorsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Job'lar yüklenirken hata: {ex.Message}";
            ErrorLogger.Log(ex, "LoadJobsInSelectedFolderAsync");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartMonitoringAsync()
    {
        if (!IsConnected || IsMonitoring) return;

        IsMonitoring = true;
        _jobMonitorTimer.Start();
        StatusMessage = "Job izleme başlatıldı - Job durumları 2 saniyede bir kontrol ediliyor";
        LogDebug("Job monitoring started");

        // İlk queue ve executor güncellemesini yap
        await UpdateQueueAndExecutorsAsync();
    }

    private void StopMonitoring()
    {
        if (!IsMonitoring) return;

        IsMonitoring = false;
        _jobMonitorTimer.Stop();

        // Queue ve running builds listelerini temizle
        QueuedBuilds.Clear();
        RunningBuilds.Clear();

        // Executor istatistiklerini sıfırla
        TotalExecutors = 0;
        BusyExecutors = 0;
        OnPropertyChanged(nameof(IdleExecutors));
        OnPropertyChanged(nameof(ExecutorSummary));

        StatusMessage = "Job izleme durduruldu";
        LogDebug("Job monitoring stopped");
    }

    private async Task MonitorJobsAsync()
    {
        if (!IsConnected || !IsMonitoring) return;

        try
        {
            LogDebug("MonitorJobsAsync - Checking job statuses");

            // Tüm job'ları kontrol et, sadece çalışan olanları değil
            var jobsToCheck = Jobs.ToList();

            if (!jobsToCheck.Any())
            {
                LogDebug("MonitorJobsAsync - No jobs to check");
                return;
            }

            LogDebug($"MonitorJobsAsync - Checking {jobsToCheck.Count} jobs");
            var hasRunningJobs = false;

            foreach (var job in jobsToCheck)
            {
                try
                {
                    // Her job için güncel durumu al
                    var jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                    var lastBuild = await _jenkinsApiService.GetLastBuildAsync(jobName);

                    if (lastBuild != null)
                    {
                        var previousStatus = job.Status;
                        var wasBuilding = job.LastBuild?.Building ?? false;
                        var wasInQueue = job.InQueue;

                        // Job'ın bilgilerini güncelle - property change notifications tetiklemek için
                        if (job.LastBuild == null || job.LastBuild.Number != lastBuild.Number || job.LastBuild.Building != lastBuild.Building)
                        {
                            job.LastBuild = lastBuild; // Bu otomatik olarak UI'ı güncelleyecek
                        }
                        else
                        {
                            // Sadece building durumu değiştiyse
                            job.LastBuild.Building = lastBuild.Building;
                        }

                        // Eğer job çalışıyorsa süre güncellemesini tetikle
                        if (job.LastBuild.Building)
                        {
                            job.LastBuild.OnPropertyChanged(nameof(job.LastBuild.DurationText));
                            job.LastBuild.OnPropertyChanged(nameof(job.LastBuild.RunningDuration));
                        }

                        // Queue status güncelleme - basit kontrol
                        // Job building değilse ve önceki durumu farklısa queue status'u kontrol et
                        if (!lastBuild.Building && job.Status != JobStatus.Building)
                        {
                            job.InQueue = false;
                        }

                        // Status değişikliklerini kontrol et
                        if (previousStatus != job.Status || wasBuilding != lastBuild.Building || wasInQueue != job.InQueue)
                        {
                            LogDebug($"MonitorJobsAsync - Job '{job.Name}' status changed: {previousStatus} -> {job.Status}, Building: {wasBuilding} -> {lastBuild.Building}, InQueue: {wasInQueue} -> {job.InQueue}");

                            if (!lastBuild.Building && wasBuilding)
                            {
                                // Build tamamlandı
                                var result = lastBuild.Result?.ToLower() == "success" ? "başarılı" : "başarısız";
                                StatusMessage = $"{job.Name} job'ı tamamlandı - {result}";
                                // Tray bildirimi göster
                                var mainWindow = System.Windows.Application.Current?.MainWindow as JenkinsAgent.MainWindow;
                                if (mainWindow != null)
                                {
                                    mainWindow.ShowNotification($"{job.Name} job'ı tamamlandı", $"Sonuç: {result}", job.Color);
                                }
                                else
                                {
                                    LogDebug("MonitorJobsAsync - MainWindow not found for notification");
                                }
                                LogDebug($"MonitorJobsAsync - Job '{job.Name}' completed with result: {lastBuild.Result}");
                            }
                            else if (lastBuild.Building && !wasBuilding)
                            {
                                // Build başladı
                                StatusMessage = $"{job.Name} job'ı çalışmaya başladı";
                                LogDebug($"MonitorJobsAsync - Job '{job.Name}' started building");
                            }
                        }

                        // Çalışan veya kuyrukta olan job var mı kontrol et
                        if (lastBuild.Building || job.InQueue)
                        {
                            hasRunningJobs = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"MonitorJobsAsync - Error checking job '{job.Name}': {ex.Message}");
                    ErrorLogger.Log(ex, $"MonitorJobsAsync - job: {job.Name}");
                }
            }

            // Queue ve executor bilgilerini güncelle
            await UpdateQueueAndExecutorsAsync();

            // Hiçbir job çalışmıyorsa monitoring'i durdur
            if (!hasRunningJobs)
            {
                LogDebug("MonitorJobsAsync - No more running jobs, stopping monitoring");
                StopMonitoring();
                StatusMessage = "Tüm job'lar tamamlandı - İzleme durduruldu";
            }
        }
        catch (Exception ex)
        {
            LogDebug($"MonitorJobsAsync - General error: {ex.Message}");
            ErrorLogger.Log(ex, "MonitorJobsAsync");
        }
    }

    private async Task UpdateQueueAndExecutorsAsync()
    {
        try
        {
            LogDebug("UpdateQueueAndExecutorsAsync - Updating queue and executor information");

            // Queue bilgilerini güncelle
            var queueItems = await _jenkinsApiService.GetQueueAsync();
            LogDebug($"UpdateQueueAndExecutorsAsync - Found {queueItems.Count} items in queue");

            QueuedBuilds.Clear();
            foreach (var queueItem in queueItems)
            {
                // Queue item'ı JenkinsJob olarak dönüştür
                var queuedJob = new JenkinsJob
                {
                    Name = queueItem.Task.Name,
                    FullPath = queueItem.Task.Name,
                    Color = queueItem.Task.Color,
                    InQueue = true,
                    IsFolder = false,
                    Description = $"Kuyrukta bekliyor: {queueItem.WaitingTime.TotalMinutes:F1} dk"
                };

                QueuedBuilds.Add(queuedJob);
                LogDebug($"UpdateQueueAndExecutorsAsync - Added queued job: {queuedJob.Name}");
            }

            // Executor bilgilerini güncelle
            var computers = await _jenkinsApiService.GetExecutorsAsync();
            LogDebug($"UpdateQueueAndExecutorsAsync - Found {computers.Count} computers");

            // Running builds'i temizleme, güncelleme yap
            int totalExecutors = 0;
            int busyExecutors = 0;

            foreach (var computer in computers)
            {
                if (computer.Offline)
                {
                    LogDebug($"UpdateQueueAndExecutorsAsync - Computer '{computer.DisplayName}' is offline, skipping");
                    continue;
                }

                totalExecutors += computer.Executors.Count;
                LogDebug($"UpdateQueueAndExecutorsAsync - Computer '{computer.DisplayName}' has {computer.Executors.Count} executors");

                foreach (var executor in computer.Executors)
                {
                    if (!executor.Idle && executor.CurrentExecutable != null)
                    {
                        busyExecutors++;

                        // Running job'ın adını çıkar
                        var jobName = ExtractJobNameFromDisplayName(executor.CurrentExecutable.FullDisplayName);

                        // Bu job'ın Jobs listesinde karşılığını bul (gerçek timestamp için)
                        var actualJob = Jobs.FirstOrDefault(j => j.Name == jobName || j.FullPath.EndsWith(jobName));

                        // Progress hesaplama
                        int displayProgress = executor.Progress;

                        // Eğer Jenkins'ten progress gelmiyorsa veya düşükse, gerçek build süresine göre tahmin et
                        if (displayProgress <= 5 && actualJob?.LastBuild != null && actualJob.LastBuild.Building)
                        {
                            try
                            {
                                var buildStartTime = actualJob.LastBuild.BuildTime;
                                var elapsed = DateTime.UtcNow - buildStartTime;

                                // Ortalama build süresi 3 dakika olarak varsay
                                var estimatedDuration = TimeSpan.FromMinutes(3);
                                var progressFromTime = Math.Min(90, (int)((elapsed.TotalSeconds / estimatedDuration.TotalSeconds) * 100));

                                // Jenkins progress ile karşılaştır, hangisi büyükse onu kullan
                                displayProgress = Math.Max(displayProgress, progressFromTime);

                                LogDebug($"UpdateQueueAndExecutorsAsync - Job '{jobName}' calculated progress: Jenkins={executor.Progress}%, Time-based={progressFromTime}%, Using={displayProgress}%");
                            }
                            catch (Exception ex)
                            {
                            LogDebug($"UpdateQueueAndExecutorsAsync - Error calculating progress for {jobName}: {ex.Message}");
                            ErrorLogger.Log(ex, $"UpdateQueueAndExecutorsAsync - progress for {jobName}");
                            // Hata durumunda basit zamana dayalı hesaplama
                            displayProgress = Math.Max(displayProgress, 10 + (DateTime.UtcNow.Second % 80));
                            }
                        }

                        var executorProgress = displayProgress > 0 ? $" - %{displayProgress}" : "";
                        var computerName = computer.DisplayName == "Built-In Node" ? "Master" : computer.DisplayName;
                        var executorKey = $"{computerName}-Executor-{executor.Number}";

                        // Mevcut job'u bul veya yeni oluştur
                        var existingJob = RunningBuilds.FirstOrDefault(j =>
                            j.Description != null && j.Description.Contains($"Executor #{executor.Number}") &&
                            j.Description.Contains(computerName));

                        if (existingJob != null)
                        {
                            // Mevcut job'u güncelle
                            var newDescription = $"{computerName} - Executor #{executor.Number}{executorProgress}";
                            if (existingJob.Description != newDescription)
                            {
                                LogDebug($"UpdateQueueAndExecutorsAsync - Updating description from '{existingJob.Description}' to '{newDescription}'");
                                existingJob.Description = newDescription;
                                // Manuel UI güncelleme tetikle
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    existingJob.OnPropertyChanged(nameof(existingJob.Description));
                                });
                                LogDebug($"UpdateQueueAndExecutorsAsync - Updated existing running build: {existingJob.Name} progress to {displayProgress}%");
                            }
                        }
                        else
                        {
                            // Yeni job oluştur
                            var runningJob = new JenkinsJob
                            {
                                Name = jobName,
                                FullPath = jobName,
                                Color = "blue_anime", // Çalışan job
                                InQueue = false,
                                IsFolder = false,
                                Description = $"{computerName} - Executor #{executor.Number}{executorProgress}",
                                LastBuild = new JenkinsBuild
                                {
                                    Building = true,
                                    Number = executor.CurrentExecutable.Number,
                                    DisplayName = executor.CurrentExecutable.FullDisplayName,
                                    Timestamp = actualJob?.LastBuild?.Timestamp ?? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                                }
                            };

                            RunningBuilds.Add(runningJob);
                            LogDebug($"UpdateQueueAndExecutorsAsync - Added new running build: {runningJob.Name} on {computerName} executor {executor.Number} (progress: {displayProgress}%)");
                        }
                    }
                    else if (!executor.Idle)
                    {
                        busyExecutors++; // Meşgul ama executable yok
                    }
                }
            }

            // Artık çalışmayan job'ları kaldır
            var currentExecutorKeys = new HashSet<string>();
            foreach (var computer in computers)
            {
                if (computer.Offline) continue;

                foreach (var executor in computer.Executors)
                {
                    if (!executor.Idle && executor.CurrentExecutable != null)
                    {
                        var computerName = computer.DisplayName == "Built-In Node" ? "Master" : computer.DisplayName;
                        currentExecutorKeys.Add($"{computerName}-Executor-{executor.Number}");
                    }
                }
            }

            var jobsToRemove = RunningBuilds.Where(j =>
                j.Description != null &&
                !currentExecutorKeys.Any(key => j.Description.Contains(key.Replace("-Executor-", " - Executor #")))).ToList();

            foreach (var jobToRemove in jobsToRemove)
            {
                RunningBuilds.Remove(jobToRemove);
                LogDebug($"UpdateQueueAndExecutorsAsync - Removed completed job: {jobToRemove.Name}");
            }

            TotalExecutors = totalExecutors;
            BusyExecutors = busyExecutors;

            // Dependent property'leri tetikle
            OnPropertyChanged(nameof(IdleExecutors));
            OnPropertyChanged(nameof(ExecutorSummary));

            LogDebug($"UpdateQueueAndExecutorsAsync - Updated: {QueuedBuilds.Count} queued, {RunningBuilds.Count} running builds on {busyExecutors}/{totalExecutors} executors");
        }
        catch (Exception ex)
        {
            LogDebug($"UpdateQueueAndExecutorsAsync error: {ex.GetType().Name}: {ex.Message}");
            ErrorLogger.Log(ex, "UpdateQueueAndExecutorsAsync");
        }
    }

    private static string ExtractJobNameFromDisplayName(string fullDisplayName)
    {
        // "FolderName » JobName #123" formatından job adını çıkar
        if (string.IsNullOrEmpty(fullDisplayName))
            return "Unknown";

        try
        {
            // " #" ile split et ve ilk kısmı al
            var parts = fullDisplayName.Split(" #");
            if (parts.Length > 0)
            {
                var jobPart = parts[0];

                // " » " ile split et ve son kısmı al (job adı)
                var jobParts = jobPart.Split(" » ");
                if (jobParts.Length > 1)
                {
                    return jobParts[jobParts.Length - 1].Trim();
                }

                return jobPart.Trim();
            }

            return fullDisplayName;
        }
        catch
        {
            return fullDisplayName;
        }
    }

    // FilterJobs method to filter jobs based on search text
    private void FilterJobs()
    {
        FilteredJobs.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            // Show all jobs if search text is empty
            foreach (var job in Jobs)
            {
                FilteredJobs.Add(job);
            }
        }
        else
        {
            var searchLower = SearchText.ToLowerInvariant();

            // Filter jobs by name, full path, or description
            foreach (var job in Jobs)
            {
                bool matches = job.Name.ToLowerInvariant().Contains(searchLower) ||
                              job.FullPath.ToLowerInvariant().Contains(searchLower) ||
                              (!string.IsNullOrEmpty(job.Description) &&
                               job.Description.ToLowerInvariant().Contains(searchLower));

                if (matches)
                {
                    FilteredJobs.Add(job);
                }
            }
        }

        OnPropertyChanged(nameof(FilteredJobsCount));
    }

    #region System Tray Methods

    private void ShowWindow()
    {
        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow != null)
        {
            // Pencereyi popup tarzında göster
            mainWindow.Show();
            mainWindow.Visibility = System.Windows.Visibility.Visible;

            // WindowState'i Normal yap (minimize'dan çıkar)
            mainWindow.WindowState = System.Windows.WindowState.Normal;

            // Pencereyi aktif et ve öne getir
            mainWindow.Activate();
            mainWindow.Focus();

            // Eğer başka uygulamalar önde ise zorla öne getir
            if (!mainWindow.IsActive)
            {
                mainWindow.Topmost = true;
                mainWindow.Topmost = false;
            }

            // Pencereyi sistem tepsisinden (saatin olduğu yerden) açılacak şekilde konumlandır
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            if (screen != null)
            {
                var workingArea = screen.WorkingArea;
                // Sistem tepsisinin tam üstüne konumlandır (saatin olduğu yer)
                mainWindow.Left = workingArea.Right - 810; // 800 + 10 margin
                mainWindow.Top = workingArea.Bottom - 610; // 600 + 10 margin
            }

            // Animasyon efekti için başlangıç pozisyonunu ayarla
            mainWindow.Opacity = 0;
            mainWindow.Topmost = true;

            // Animasyon ile görünür hale getir
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            mainWindow.BeginAnimation(System.Windows.UIElement.OpacityProperty, animation);
        }
    }

    private void ExitApplication()
    {
        // Monitoring'i durdur
        StopMonitoring();
        System.Windows.Application.Current.Shutdown();
    }

    #endregion

    private Task OpenBlueOceanAsync(JenkinsJob? job)
    {
        if (job == null || !IsConnected || string.IsNullOrEmpty(_jenkinsConfig.BaseUrl)) return Task.CompletedTask;

        try
        {
            // Blue Ocean URL'ini oluştur - doğru format
            var baseUrl = _jenkinsConfig.BaseUrl.TrimEnd('/');
            string blueOceanUrl;

            // Job path'ini Blue Ocean format'ına çevir
            if (SelectedFolder != null && !string.IsNullOrEmpty(SelectedFolder.Name) && SelectedFolder.Name != "Tüm Job'lar")
            {
                // Folder içindeki job için - örnek: /blue/organizations/jenkins/EGEM%2FANGULAR%20UAT%20(Web%20Deploy)/detail/ANGULAR%20UAT%20(Web%20Deploy)/17/pipeline
                var folderJobPath = $"{SelectedFolder.Name}/{job.Name}";
                var encodedPath = Uri.EscapeDataString(folderJobPath);
                var encodedJobName = Uri.EscapeDataString(job.Name);

                // Eğer son build varsa, detaylı pipeline view, yoksa activity
                if (job.LastBuild?.Number > 0)
                {
                    blueOceanUrl = $"{baseUrl}/blue/organizations/jenkins/{encodedPath}/detail/{encodedJobName}/{job.LastBuild.Number}/pipeline";
                }
                else
                {
                    blueOceanUrl = $"{baseUrl}/blue/organizations/jenkins/{encodedPath}/activity/";
                }
            }
            else
            {
                // Root level job için
                var jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                var encodedJobName = Uri.EscapeDataString(jobName);

                // Eğer son build varsa, detaylı pipeline view, yoksa activity
                if (job.LastBuild?.Number > 0)
                {
                    blueOceanUrl = $"{baseUrl}/blue/organizations/jenkins/detail/{encodedJobName}/{job.LastBuild.Number}/pipeline";
                }
                else
                {
                    blueOceanUrl = $"{baseUrl}/blue/organizations/jenkins/{encodedJobName}/activity/";
                }
            }

            LogDebug($"Opening Blue Ocean URL: {blueOceanUrl}");
            StatusMessage = $"{job.Name} için Blue Ocean açılıyor...";

            // BlueOceanWindow'u aç
            var blueOceanWindow = new Views.BlueOceanWindow(blueOceanUrl, job.Name);
            blueOceanWindow.Show();

            StatusMessage = $"{job.Name} için Blue Ocean açıldı";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Blue Ocean açılırken hata: {ex.Message}";
            LogDebug($"OpenBlueOceanAsync exception: {ex.GetType().Name}: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private Task ConfigureJobAsync(JenkinsJob? job)
    {
        if (job == null || !IsConnected || string.IsNullOrEmpty(_jenkinsConfig.BaseUrl)) return Task.CompletedTask;

        try
        {
            // Jenkins job configure URL'sini oluştur
            var baseUrl = _jenkinsConfig.BaseUrl.TrimEnd('/');
            string configureUrl;

            // Job path'ini configure format'ına çevir
            if (SelectedFolder != null && !string.IsNullOrEmpty(SelectedFolder.Name) && SelectedFolder.Name != "Tüm Job'lar")
            {
                // Folder içindeki job için - Jenkins'te her seviye için /job/ prefix'i gerekli
                var folderJobPath = $"{SelectedFolder.Name}/{job.Name}";
                var pathSegments = folderJobPath.Split('/');
                var jenkinsPath = string.Join("/job/", pathSegments);
                configureUrl = $"{baseUrl}/job/{jenkinsPath}/configure";
            }
            else
            {
                // Root level job için
                var jobName = !string.IsNullOrEmpty(job.FullPath) ? job.FullPath : job.Name;
                configureUrl = $"{baseUrl}/job/{jobName}/configure";
            }

            LogDebug($"Opening Configure URL: {configureUrl}");
            StatusMessage = $"{job.Name} için Configure açılıyor...";

            // Varsayılan tarayıcıda aç
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = configureUrl,
                UseShellExecute = true
            });

            StatusMessage = $"{job.Name} için Configure açıldı";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Configure açılırken hata: {ex.Message}";
            LogDebug($"ConfigureJobAsync exception: {ex.GetType().Name}: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private void OpenSettings()
    {
        try
        {
            var settingsWindow = new Views.SettingsWindow()
            {
                ShowInTaskbar = false
            };
            var settingsViewModel = new SettingsWindowViewModel(
                _jenkinsApiService,
                _settingsService,
                async () =>
                {
                    await LoadSettingsAsync();
                    await CheckAndAutoConnectAsync();
                    StatusMessage = "Ayarlar güncellendi";
                });

            settingsWindow.DataContext = settingsViewModel;
            settingsWindow.Show();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar penceresi açılırken hata: {ex.Message}";
        }
    }

    private void OpenAbout()
    {
        try
        {
            var aboutWindow = new Views.AboutWindow
            {
                ShowInTaskbar = false
            };
            aboutWindow.Show();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Hakkında penceresi açılırken hata: {ex.Message}";
        }
    }

    private Task OpenJenkinsHomeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(JenkinsConfig.BaseUrl))
            {
                StatusMessage = "Jenkins URL'si bulunamadı";
                return Task.CompletedTask;
            }

            var url = JenkinsConfig.BaseUrl.TrimEnd('/');
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(processInfo);
            StatusMessage = "Jenkins ana sayfası açılıyor...";
        }
        catch (Exception ex)
        {
            LogDebug($"Jenkins ana sayfası açılırken hata: {ex.Message}");
            StatusMessage = "Jenkins ana sayfası açılamadı";
        }
        return Task.CompletedTask;
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();
            JenkinsConfig = settings.Jenkins;
            LogDebug($"LoadSettingsAsync - Loaded Jenkins config. BaseUrl: '{settings.Jenkins.BaseUrl}', Username: '{settings.Jenkins.Username}'");
            LogDebug($"LoadSettingsAsync - LastSelectedFolder: '{settings.LastSelectedFolder}', LastSelectedFolderFullPath: '{settings.LastSelectedFolderFullPath}'");
            LogDebug($"LoadSettingsAsync - Settings file location: {_settingsService.GetSettingsFilePath()}");
        }
        catch (Exception ex)
        {
            LogDebug($"LoadSettingsAsync - Error loading settings: {ex.Message}");
            LogDebug($"LoadSettingsAsync - Using default settings");
        }
    }

    private async Task CheckAndAutoConnectAsync()
    {
        // Check if all required settings are configured
        bool hasValidSettings = !string.IsNullOrWhiteSpace(JenkinsConfig.BaseUrl) &&
                               !string.IsNullOrWhiteSpace(JenkinsConfig.Username) &&
                               !string.IsNullOrWhiteSpace(JenkinsConfig.ApiToken);

        if (hasValidSettings)
        {
            // Settings are configured, hide warning and show jobs section
            ShowSettingsWarning = false;
            ShowJobsSection = true;

            // Auto-connect
            StatusMessage = "Otomatik bağlanıyor...";
            await ConnectAsync();
        }
        else
        {
            // Settings not configured, show warning and hide jobs section
            ShowSettingsWarning = true;
            ShowJobsSection = false;
            StatusMessage = "Jenkins ayarları yapılandırılmamış";
        }
    }


}

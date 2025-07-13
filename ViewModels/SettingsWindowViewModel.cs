using JenkinsAgent.Commands;
using JenkinsAgent.Models;
using JenkinsAgent.Services;
using Microsoft.Win32;
using System.Reflection;
using System.Windows.Media;


namespace JenkinsAgent.ViewModels;

public class SettingsWindowViewModel : BaseViewModel
{
    private readonly IJenkinsApiService _jenkinsApiService;
    private readonly SettingsService _settingsService;
    private readonly Action _onSettingsSaved;

    private JenkinsConfig _jenkinsConfig = new();
    private bool _isTesting;
    private bool _isConnected;
    private string _testStatusMessage = "";
    private Brush _testStatusColor = Brushes.Gray;
    private string _statusMessage = "Ayarları düzenleyin";
    private string _apiToken = "";
    private bool _minimizeToTray = true;
    private bool _startWithWindows = false;
    private int _monitoringInterval = 5;
    private JenkinsJob? _selectedDefaultFolder;


    public JenkinsConfig JenkinsConfig
    {
        get => _jenkinsConfig;
        set => SetProperty(ref _jenkinsConfig, value);
    }

    public string ApiToken
    {
        get => _apiToken;
        set
        {
            if (SetProperty(ref _apiToken, value))
            {
                JenkinsConfig.ApiToken = value;
            }
        }
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public bool IsNotTesting => !IsTesting;

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public bool IsNotConnected => !IsConnected;

    public string TestStatusMessage
    {
        get => _testStatusMessage;
        set => SetProperty(ref _testStatusMessage, value);
    }

    public Brush TestStatusColor
    {
        get => _testStatusColor;
        set => SetProperty(ref _testStatusColor, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (SetProperty(ref _startWithWindows, value))
            {
                SetStartupRegistry(value);
            }
        }
    }

    public int MonitoringInterval
    {
        get => _monitoringInterval;
        set => SetProperty(ref _monitoringInterval, value);
    }

    public JenkinsJob? SelectedDefaultFolder
    {
        get => _selectedDefaultFolder;
        set => SetProperty(ref _selectedDefaultFolder, value);
    }

    public ObservableCollection<JenkinsJob> AvailableFolders { get; } = new();



    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetToDefaultCommand { get; }

    public SettingsWindowViewModel(IJenkinsApiService jenkinsApiService, SettingsService settingsService, Action onSettingsSaved)
    {
        _jenkinsApiService = jenkinsApiService;
        _settingsService = settingsService;
        _onSettingsSaved = onSettingsSaved;

        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsTesting);
        SaveCommand = new RelayCommand(async () => await SaveSettingsAsync());
        CancelCommand = new RelayCommand(CloseWindow);
        ResetToDefaultCommand = new RelayCommand(ResetToDefault);

        LoadCurrentSettings();
    }

    private async void LoadCurrentSettings()
    {
        try
        {
            var settings = await _settingsService.LoadSettingsAsync();

            JenkinsConfig = new JenkinsConfig
            {
                BaseUrl = settings.Jenkins.BaseUrl,
                Username = settings.Jenkins.Username,
                ApiToken = settings.Jenkins.ApiToken
            };

            ApiToken = settings.Jenkins.ApiToken;
            MinimizeToTray = settings.MinimizeToTray;

            _startWithWindows = GetStartupRegistry();
            OnPropertyChanged(nameof(StartWithWindows));
            MonitoringInterval = settings.MonitoringIntervalSeconds;


            StatusMessage = "Mevcut ayarlar yüklendi";

            if (!string.IsNullOrEmpty(JenkinsConfig.BaseUrl) &&
                !string.IsNullOrEmpty(JenkinsConfig.Username) &&
                !string.IsNullOrEmpty(JenkinsConfig.ApiToken))
            {
                await LoadFoldersAsync();

                if (!string.IsNullOrEmpty(settings.LastSelectedFolder))
                {
                    var savedFolder = AvailableFolders.FirstOrDefault(f =>
                        f.Name == settings.LastSelectedFolder &&
                        f.FullPath == settings.LastSelectedFolderFullPath);

                    if (savedFolder != null)
                    {
                        SelectedDefaultFolder = savedFolder;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar yüklenirken hata: {ex.Message}";
        }
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestStatusMessage = "Bağlantı test ediliyor...";
        TestStatusColor = Brushes.Blue;

        try
        {
            if (string.IsNullOrWhiteSpace(JenkinsConfig.BaseUrl))
            {
                TestStatusMessage = "Sunucu URL'si gerekli";
                TestStatusColor = Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(JenkinsConfig.Username))
            {
                TestStatusMessage = "Kullanıcı adı gerekli";
                TestStatusColor = Brushes.Red;
                return;
            }

            if (string.IsNullOrWhiteSpace(ApiToken))
            {
                TestStatusMessage = "API Token gerekli";
                TestStatusColor = Brushes.Red;
                return;
            }

            if (!JenkinsConfig.BaseUrl.StartsWith("http://") && !JenkinsConfig.BaseUrl.StartsWith("https://"))
            {
                JenkinsConfig.BaseUrl = "http://" + JenkinsConfig.BaseUrl;
            }

            JenkinsConfig.ApiToken = ApiToken;

            _jenkinsApiService.UpdateConfiguration(JenkinsConfig);
            var result = await _jenkinsApiService.TestConnectionAsync();

            if (result)
            {
                TestStatusMessage = "Bağlantı başarılı!";
                TestStatusColor = Brushes.Green;
                IsConnected = true;
                StatusMessage = "Bağlantı başarılı - Klasörler yükleniyor...";

                await LoadFoldersAsync();
            }
            else
            {
                TestStatusMessage = "Bağlantı başarısız";
                TestStatusColor = Brushes.Red;
                IsConnected = false;
                StatusMessage = "Bağlantı başarısız - URL, kullanıcı adı ve API token'ı kontrol edin";
            }
        }
        catch (Exception ex)
        {
            TestStatusMessage = $"Hata: {ex.Message}";
            TestStatusColor = Brushes.Red;
            IsConnected = false;
            StatusMessage = $"Bağlantı hatası: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private async Task LoadFoldersAsync()
    {
        try
        {
            var folders = await _jenkinsApiService.GetFoldersAsync();

            AvailableFolders.Clear();

            foreach (var folder in folders)
            {
                AvailableFolders.Add(folder);
            }

            StatusMessage = $"Bağlantı başarılı - {folders.Count} klasör yüklendi";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Klasörler yüklenirken hata: {ex.Message}";
        }
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            StatusMessage = "Ayarlar kaydediliyor...";

            if (MonitoringInterval < 1 || MonitoringInterval > 300)
            {
                StatusMessage = "İzleme aralığı 1-300 saniye arasında olmalıdır";
                return;
            }

            var settings = await _settingsService.LoadSettingsAsync();

            settings.Jenkins = new JenkinsConfig
            {
                BaseUrl = JenkinsConfig.BaseUrl,
                Username = JenkinsConfig.Username,
                ApiToken = ApiToken
            };

            settings.MinimizeToTray = MinimizeToTray;
            settings.StartWithWindows = StartWithWindows;
            settings.MonitoringIntervalSeconds = MonitoringInterval;


            if (SelectedDefaultFolder != null)
            {
                settings.LastSelectedFolder = SelectedDefaultFolder.Name;
                settings.LastSelectedFolderFullPath = SelectedDefaultFolder.FullPath ?? "";
            }

            await _settingsService.SaveSettingsAsync(settings);

            StatusMessage = "Ayarlar başarıyla kaydedildi";

            _onSettingsSaved?.Invoke();

            await Task.Delay(1000);
            CloseWindow();


        }
        catch (Exception ex)
        {
            StatusMessage = $"Ayarlar kaydedilirken hata: {ex.Message}";
        }
    }

    private void ResetToDefault()
    {
        JenkinsConfig = new JenkinsConfig();
        ApiToken = "";
        MinimizeToTray = true;
        StartWithWindows = false;
        MonitoringInterval = 5;
        SelectedDefaultFolder = null;
        IsConnected = false;
        TestStatusMessage = "";
        TestStatusColor = Brushes.Gray;
        AvailableFolders.Clear();
        StatusMessage = "Ayarlar varsayılan değerlere sıfırlandı";
    }

    private void CloseWindow()
    {
        var window = System.Windows.Application.Current.Windows
            .OfType<Views.SettingsWindow>()
            .FirstOrDefault(w => w.DataContext == this);

        window?.Close();
    }

    public override void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsTesting))
        {
            OnPropertyChanged(nameof(IsNotTesting));
        }

        if (propertyName == nameof(IsConnected))
        {
            OnPropertyChanged(nameof(IsNotConnected));
        }
    }

    #region Startup Management

    private const string STARTUP_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string APP_NAME = "JenkinsAgent";

    private void SetStartupRegistry(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(STARTUP_KEY, true);
            if (key != null)
            {
                if (enable)
                {
                    string? exePath = null;
                    try { exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName; } catch { }
                    if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exePath = Environment.ProcessPath;
                    }
                    if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exePath = Assembly.GetEntryAssembly()?.Location;
                    }
                    if (string.IsNullOrEmpty(exePath) || !exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        exePath = Assembly.GetExecutingAssembly().Location;
                    }

                    if (!string.IsNullOrEmpty(exePath) && exePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        key.SetValue(APP_NAME, $"\"{exePath}\"");
                        StatusMessage = "Windows ile birlikte başlatma aktif edildi";
                    }
                    else
                    {
                        StatusMessage = "Uygulama .exe yolu bulunamadı, Windows ile başlatılamaz.";
                    }
                }
                else
                {
                    key.DeleteValue(APP_NAME, false);
                    StatusMessage = "Windows ile birlikte başlatma devre dışı bırakıldı";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Başlangıç ayarı güncellenirken hata: {ex.Message}";
        }
    }

    private bool GetStartupRegistry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(STARTUP_KEY, false);
            return key?.GetValue(APP_NAME) != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}

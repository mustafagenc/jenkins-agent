using Microsoft.Web.WebView2.Core;
using System.Runtime.CompilerServices;
using System.Windows;

namespace JenkinsAgent.Views;

/// <summary>
/// Blue Ocean interface for Jenkins jobs
/// </summary>
public partial class BlueOceanWindow : Window, INotifyPropertyChanged
{
    private string _jobName = string.Empty;
    private string _statusMessage = "Hazırlanıyor...";
    private bool _isLoading = true;
    private string _loadingText = string.Empty;
    private string _blueOceanUrl = string.Empty;

    public string JobName
    {
        get => _jobName;
        set => SetProperty(ref _jobName, value);
    }

    public string WindowTitle => $"Blue Ocean - {JobName}";

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string LoadingText
    {
        get => _loadingText;
        set => SetProperty(ref _loadingText, value);
    }

    public BlueOceanWindow(string blueOceanUrl, string jobName)
    {
        InitializeComponent();
        DataContext = this;

        _blueOceanUrl = blueOceanUrl;
        JobName = jobName;
        StatusMessage = "Blue Ocean yükleniyor...";
        LoadingText = "Bağlanıyor...";

        InitializeWebView();
    }

    private async void InitializeWebView()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var userDataFolder = System.IO.Path.Combine(appData, "JenkinsAgent", "WebView2UserData");
            System.IO.Directory.CreateDirectory(userDataFolder);
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await BlueOceanWebView.EnsureCoreWebView2Async(env);
            BlueOceanWebView.CoreWebView2.Navigate(_blueOceanUrl);
        }
        catch (Exception ex)
        {
            StatusMessage = $"WebView2 başlatılamadı: {ex.Message}";
            IsLoading = false;
            LoadingText = "Hata";
        }
    }

    private void BlueOceanWebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
    {
        if (e.IsSuccess)
        {
            StatusMessage = "WebView2 hazır";
            LoadingText = "Sayfa yükleniyor...";

            BlueOceanWebView.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 JenkinsAgent/1.0";

            BlueOceanWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            BlueOceanWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            BlueOceanWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            BlueOceanWebView.CoreWebView2.Settings.AreHostObjectsAllowed = false;
        }
        else
        {
            StatusMessage = $"WebView2 başlatılamadı: {e.InitializationException?.Message}";
            IsLoading = false;
            LoadingText = "Hata";
        }
    }

    private void BlueOceanWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        IsLoading = true;
        LoadingText = "Yükleniyor...";
        StatusMessage = $"Sayfa yükleniyor: {e.Uri}";
    }

    private async void BlueOceanWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        IsLoading = false;
        LoadingText = string.Empty;

        if (e.IsSuccess)
        {
            StatusMessage = "Blue Ocean yüklendi";

            await HideJenkinsHeaderAsync();
        }
        else
        {
            StatusMessage = $"Sayfa yüklenemedi: {e.WebErrorStatus}";
        }
    }

    private async Task HideJenkinsHeaderAsync()
    {
        try
        {
            var hideHeaderScript = @"
                function hideHeaders() {
                    console.log('Attempting to hide Jenkins headers...');
                    
                    const headerSelectors = [
                        '.Header-topNav',
                        '.Header',
                        '#header',
                        '.header-search-bar',
                        '.jenkins-header',
                        '.app-bar',
                        '.toolbar',
                        'nav[role=""navigation""]',
                        '[data-testid=""header""]'
                    ];
                    
                    let hiddenCount = 0;
                    headerSelectors.forEach(selector => {
                        const elements = document.querySelectorAll(selector);
                        elements.forEach(element => {
                            if (element) {
                                element.style.display = 'none';
                                element.style.visibility = 'hidden';
                                element.style.height = '0';
                                element.style.overflow = 'hidden';
                                hiddenCount++;
                                console.log('Hidden element:', selector);
                            }
                        });
                    });
                    
                    console.log('Total hidden elements:', hiddenCount);
                    return hiddenCount;
                }

                function injectCSS() {
                    const existingStyle = document.getElementById('jenkins-header-hider');
                    if (existingStyle) {
                        existingStyle.remove();
                    }
                    
                    const style = document.createElement('style');
                    style.id = 'jenkins-header-hider';
                    style.textContent = `
                        .Header-topNav,
                        .Header,
                        #header,
                        .header-search-bar,
                        .jenkins-header,
                        .app-bar,
                        .toolbar,
                        nav[role=""navigation""],
                        [data-testid=""header""] {
                            display: none !important;
                            visibility: hidden !important;
                            height: 0 !important;
                            overflow: hidden !important;
                            margin: 0 !important;
                            padding: 0 !important;
                        }
                        
                        /* Blue Ocean content'i yukarı taşı */
                        .Site,
                        .Site-content,
                        main,
                        .main-content,
                        .app-content {
                            margin-top: 0 !important;
                            padding-top: 0 !important;
                        }
                        
                        /* Body scroll ayarları - tek scroll bar */
                        html, body {
                            margin: 0 !important;
                            padding: 0 !important;
                            height: 100vh !important;
                            overflow: auto !important;
                            scrollbar-width: thin !important;
                        }
                        
                        /* Container'ların scroll ayarları */
                        .App, .app, .main-container, .content-container {
                            height: 100vh !important;
                            overflow: visible !important;
                        }
                        
                        /* Custom scrollbar styling */
                        ::-webkit-scrollbar {
                            width: 8px !important;
                        }
                        
                        ::-webkit-scrollbar-track {
                            background: #f1f1f1 !important;
                        }
                        
                        ::-webkit-scrollbar-thumb {
                            background: #c1c1c1 !important;
                            border-radius: 4px !important;
                        }
                        
                        ::-webkit-scrollbar-thumb:hover {
                            background: #a8a8a8 !important;
                        }
                    `;
                    document.head.appendChild(style);
                    console.log('CSS injected for header hiding and scroll management');
                }

                function setupMutationObserver() {
                    const observer = new MutationObserver(function(mutations) {
                        let shouldHide = false;
                        mutations.forEach(function(mutation) {
                            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                                shouldHide = true;
                            }
                        });
                        
                        if (shouldHide) {
                            setTimeout(() => {
                                hideHeaders();
                            }, 100);
                        }
                    });
                    
                    observer.observe(document.body, {
                        childList: true,
                        subtree: true
                    });
                    
                    console.log('Mutation observer setup complete');
                }

                function initHeaderHiding() {
                    injectCSS();
                    
                    hideHeaders();
                    
                    setupMutationObserver();
                    
                    setInterval(() => {
                        hideHeaders();
                    }, 2000);
                    
                    console.log('Header hiding initialized');
                }

                if (document.readyState === 'loading') {
                    document.addEventListener('DOMContentLoaded', initHeaderHiding);
                } else {
                    initHeaderHiding();
                }

                window.addEventListener('load', () => {
                    setTimeout(initHeaderHiding, 500);
                });
            ";

            await BlueOceanWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(hideHeaderScript);
            await BlueOceanWebView.CoreWebView2.ExecuteScriptAsync(hideHeaderScript);

        }
        catch (Exception ex)
        {
            StatusMessage = $"Header gizleme hatası: {ex.Message}";
        }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BlueOceanWebView.CoreWebView2?.Reload();
            StatusMessage = "Sayfa yenileniyor...";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Yenileme hatası: {ex.Message}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            if (propertyName == nameof(JobName))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WindowTitle)));
            }
        }
    }
}

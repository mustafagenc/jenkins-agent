using JenkinsAgent.Models;
using JenkinsAgent.Services;
using JenkinsAgent.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using System.Windows.Threading;


namespace JenkinsAgent;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Global hata yakalama
        Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    // Models - Factory pattern kullanarak
                    services.AddSingleton<JenkinsConfig>(provider => new JenkinsConfig());
                    services.AddSingleton<AppSettings>(provider => new AppSettings());

                    // Services
                    services.AddSingleton<SettingsService>();
                    services.AddHttpClient();
                    services.AddSingleton<IJenkinsApiService>(provider =>
                    {
                        var jenkinsConfig = provider.GetRequiredService<JenkinsConfig>();
                        // HttpClient'ı direkt oluşturalım
                        var httpClient = new HttpClient();
                        return new JenkinsApiService(httpClient, jenkinsConfig);
                    });

                    // ViewModels
                    services.AddTransient<MainWindowViewModel>();

                    // Views
                    services.AddTransient<MainWindow>();
                })
                .Build();

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            // Başlangıçta pencereyi gizle, sadece sistem tepsisinde görünsün
            // mainWindow.Show(); // Bu satırı kaldırdık

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            JenkinsAgent.ViewModels.ErrorLogger.Log(ex, "App.OnStartup");
            MessageBox.Show($"Uygulama başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }



    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        JenkinsAgent.ViewModels.ErrorLogger.Log(e.Exception, "App.DispatcherUnhandledException");
        ShowErrorDialog("WPF Hatası", e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            JenkinsAgent.ViewModels.ErrorLogger.Log(ex, "App.UnhandledException");
            ShowErrorDialog("Uygulama Hatası", ex);
        }
    }

    private void ShowErrorDialog(string title, Exception exception)
    {
        try
        {
            var errorMessage = $"Hata: {exception.Message}\n\nDetaylar:\n{exception.StackTrace}";

            var result = MessageBox.Show(
                errorMessage,
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Error);

            if (result == MessageBoxResult.Cancel)
            {
                Shutdown();
            }
        }
        catch
        {
            // Hata dialog'u da başarısız olursa uygulamayı kapat
            Shutdown();
        }
    }
}


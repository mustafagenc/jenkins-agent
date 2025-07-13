using JenkinsAgent.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JenkinsAgent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // ViewModel'dan çağrılabilen tray bildirim fonksiyonu
    public void ShowNotification(string title, string message, string? color = null)
    {
        // Renk parametresi şimdilik kullanılmıyor, istenirse ikon veya balon rengi için eklenebilir
        ShowTrayNotification(title, message);
    }

    // Sistem tepsisi balon bildirimi gösterir
    private static void ShowTrayNotification(string title, string message)
    {
        // NotifyIcon sadece WinForms'da var, WPF'de elle eklenmeli
        var notifyIcon = new System.Windows.Forms.NotifyIcon();
        notifyIcon.BalloonTipTitle = title;
        notifyIcon.BalloonTipText = message;
        notifyIcon.Visible = true;
        // Uygulama ikonu varsa kullan
        try
        {
            notifyIcon.Icon = new System.Drawing.Icon("Resources/jenkins.ico");
        }
        catch { }
        notifyIcon.ShowBalloonTip(3000);
        void handler(object? s, System.EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            notifyIcon.BalloonTipClosed -= handler;
        }
        notifyIcon.BalloonTipClosed += handler;
        var timer = new System.Timers.Timer(5000);
        timer.Elapsed += (s, e) =>
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            timer.Dispose();
        };
        timer.AutoReset = false;
        timer.Start();
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            DataContext = viewModel;
            Title = "Jenkins Agent";

            // Pencereyi sistem tepsisine yakın konumlandır
            PositionWindowNearTray();

            // Minimize olduğunda sistem tepsisine gizle
            StateChanged += (s, e) =>
            {
                if (WindowState == WindowState.Minimized)
                {
                    Hide();
                }
            };

            // Pencere dışına tıklandığında gizle
            this.Deactivated += (s, e) => this.Hide();

            // Güncelleme kontrolü
            CheckForUpdate();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"MainWindow başlatılırken hata: {ex.Message}\n\nDetaylar:\n{ex.StackTrace}",
                          "Başlatma Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private async void CheckForUpdate()
    {
        try
        {
            bool update = await Services.UpdateService.IsUpdateAvailableAsync();
            if (update)
            {
                var remoteInfo = await Services.UpdateService.GetRemoteUpdateInfoAsync();
                if (remoteInfo == null || string.IsNullOrWhiteSpace(remoteInfo.DownloadUrl))
                {
                    MessageBox.Show("Sürüm kontrol bağlantısına erişilemiyor. Uygulama başlatılamadı.", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                    return;
                }
                var local = Services.UpdateService.GetLocalVersion();
                string link = remoteInfo.DownloadUrl;
                string msg = $"Yeni bir sürüm mevcut!\n\nYüklü sürüm: {local}\nSon sürüm: {remoteInfo.Version}";
                // Modern pop-up pencereyi oluştur
                var win = new Window
                {
                    Title = "Güncelleme Uyarısı",
                    Width = 380,
                    Height = 240,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    Topmost = true,
                    Content = BuildUpdateContent(msg, link),
                    Background = Brushes.Transparent,
                    AllowsTransparency = true
                };
                // Sistem tepsisine yakın konumlandır
                var screen = System.Windows.Forms.Screen.PrimaryScreen;
                var workingArea = screen != null ? screen.WorkingArea : new System.Drawing.Rectangle(0, 0, 1024, 768);
                win.Left = workingArea.Right - win.Width - 16;
                win.Top = workingArea.Bottom - win.Height - 16;
                win.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sürüm kontrolü sırasında hata oluştu.\n{ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
    }

    private static UIElement BuildUpdateContent(string msg, string link)
    {
        var mainGrid = new Grid
        {
            Width = 340,
            Height = 220
        };
        var headerGrid = new Grid
        {
            Margin = new Thickness(0),
        };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var headerText = new TextBlock
        {
            Text = "Jenkins Agent",
            Foreground = Brushes.White,
            FontWeight = FontWeights.Bold,
            FontSize = 20,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(headerText, 1);
        headerGrid.Children.Add(headerText);
        var header = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
            CornerRadius = new CornerRadius(10, 10, 0, 0),
            Height = 50,
            VerticalAlignment = VerticalAlignment.Top,
            Child = headerGrid
        };
        mainGrid.Children.Add(header);

        var dock = new Grid
        {
            Background = new SolidColorBrush(Color.FromRgb(255, 255, 255)),
            Margin = new Thickness(0, 30, 0, 0),
            Width = 340,
            Height = 150
        };
        dock.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
        dock.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        dock.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

        var contentPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
        var infoImg = new Image
        {
            Width = 64,
            Height = 64,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 22, 0),
            Stretch = Stretch.Uniform,
            Source = new BitmapImage(new Uri("pack://application:,,,/Resources/update.png", UriKind.Absolute))
        };
        contentPanel.Children.Add(infoImg);
        var textStack = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
        var lines = msg.Split('\n');
        foreach (var line in lines)
        {
            textStack.Children.Add(new TextBlock
            {
                Text = line,
                FontSize = 14,
                Foreground = Brushes.Black,
                Margin = new Thickness(0, 0, 0, 2),
                TextWrapping = TextWrapping.Wrap
            });
        }
        contentPanel.Children.Add(textStack);
        Grid.SetRow(contentPanel, 0);
        dock.Children.Add(contentPanel);

        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
        if (!string.IsNullOrWhiteSpace(link))
        {
            var indirBtn = new Button
            {
                Content = "Yeni Sürümü İndir",
                Width = 140,
                Height = 32,
                Margin = new Thickness(0, 0, 20, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                BorderBrush = null,
                Style = (Style)Application.Current.TryFindResource("Button"),
                Opacity = 0.98
            };
            indirBtn.Click += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link) { UseShellExecute = true });
                }
                catch { }
                Application.Current.Shutdown();
            };
            btnPanel.Children.Add(indirBtn);
        }
        var kapatBtn = new Button
        {
            Content = "Kapat",
            Width = 80,
            Height = 32,
            Background = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
            Foreground = Brushes.Black,
            FontWeight = FontWeights.Normal,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            BorderBrush = null,
            Style = (Style)Application.Current.TryFindResource("Button"),
            Opacity = 0.98
        };
        kapatBtn.Click += (s, e) =>
        {
            Application.Current.Shutdown();
        };
        btnPanel.Children.Add(kapatBtn);
        Grid.SetRow(btnPanel, 2);
        dock.Children.Add(btnPanel);

        mainGrid.Children.Add(dock);

        var outer = new Border
        {
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Child = mainGrid,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.18
            }
        };
        return outer;
    }
    /// <summary>
    /// Pencereyi sistem tepsisine yakın konumlandırır
    /// </summary>
    private void PositionWindowNearTray()
    {
        // Sistem tepsisinin konumunu al
        var screen = System.Windows.Forms.Screen.PrimaryScreen;
        var workingArea = screen != null ? screen.WorkingArea : new System.Drawing.Rectangle(0, 0, 1024, 768);

        // Pencere boyutları
        var windowWidth = 800;
        var windowHeight = 600;

        // Sağ alt köşeye konumlandır (sistem tepsisinin yanına)
        this.Left = workingArea.Right - windowWidth - 10;
        this.Top = workingArea.Bottom - windowHeight - 10;
    }

    /// <summary>
    /// Pencereyi belirtilen konumda gösterir
    /// </summary>
    /// <param name="x">X koordinatı</param>
    /// <param name="y">Y koordinatı</param>
    public void ShowAtPosition(double x, double y)
    {
        this.Left = x;
        this.Top = y;
        this.Show();
        this.Activate();
    }

    protected override async void OnClosed(EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SaveSettingsAsync();
        }
        base.OnClosed(e);
    }

    /// <summary>
    /// Sistem tepsisindeki simgeye sol tıklandığında uygulamayı gösterir
    /// </summary>
    private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
    {
        // Eğer pencere görünür ve normal durumdaysa gizle
        if (this.Visibility == Visibility.Visible && this.WindowState == WindowState.Normal)
        {
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }
        else
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Focus();
        }
    }

    /// <summary>
    /// ESC tuşuna basıldığında pencereyi simge durumuna küçültür
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.WindowState = WindowState.Minimized;
            e.Handled = true;
        }
    }
}
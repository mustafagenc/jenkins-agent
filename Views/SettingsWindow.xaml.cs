using System.Windows;
using System.Windows.Controls;

namespace JenkinsAgent.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        this.PreviewKeyDown += Window_KeyDown;
        this.Deactivated += (s, e) => this.Hide();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var workingArea = SystemParameters.WorkArea;
        Left = workingArea.Right - Width - 16;
        Top = workingArea.Bottom - Height - 16;

        if (DataContext is ViewModels.SettingsWindowViewModel viewModel)
        {
            ApiTokenBox.Password = viewModel.ApiToken;
        }
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            this.Hide();
            e.Handled = true;
        }
    }

    private void ApiTokenBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsWindowViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.ApiToken = passwordBox.Password;
        }
    }
}

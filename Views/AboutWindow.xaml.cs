using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace JenkinsAgent.Views
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            this.PreviewKeyDown += Window_KeyDown;
            this.Deactivated += (s, e) => this.Hide();
            SetVersionText();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width - 16;
            Top = workingArea.Bottom - Height - 16;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Hide();
                e.Handled = true;
            }
        }

        private void SetVersionText()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "?";
            VersionTextBlock.Text = $"Sürüm: {version}";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

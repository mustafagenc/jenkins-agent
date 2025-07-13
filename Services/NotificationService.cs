using System.Windows.Forms;

namespace JenkinsAgent.Services
{
    public static class NotificationService
    {
        private static NotifyIcon? _notifyIcon;
        private static System.Windows.Forms.Timer? _hideTimer;

        public static void Show(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _notifyIcon ??= new NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Information
                };

                _notifyIcon.Visible = true;
                _notifyIcon.BalloonTipTitle = title;
                _notifyIcon.BalloonTipText = message;
                _notifyIcon.BalloonTipIcon = icon;
                _notifyIcon.ShowBalloonTip(8000);

                _hideTimer?.Stop();
                _hideTimer?.Dispose();

                _hideTimer = new System.Windows.Forms.Timer
                {
                    Interval = 9000
                };
                _hideTimer.Tick += (s, e) =>
                {
                    if (_notifyIcon != null)
                        _notifyIcon.Visible = false;
                    _hideTimer?.Stop();
                    _hideTimer?.Dispose();
                    _hideTimer = null;
                };
                _hideTimer.Start();
            }
            catch
            {
            }
        }
    }
}

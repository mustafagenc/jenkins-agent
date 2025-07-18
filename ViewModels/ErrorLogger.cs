namespace JenkinsAgent.ViewModels
{
    public static class ErrorLogger
    {
        private static readonly string ErrorLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.txt");

        public static void Log(Exception ex, string? context = null)
        {
            try
            {
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {context ?? "Error"}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n";
                if (!File.Exists(ErrorLogPath))
                {
                    using (File.Create(ErrorLogPath)) { }
                }
                File.AppendAllText(ErrorLogPath, message);
            }
            catch
            {
            }
        }

        public static void Log(string message)
        {
            try
            {
                var log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n";
                if (!File.Exists(ErrorLogPath))
                {
                    using (File.Create(ErrorLogPath)) { }
                }
                File.AppendAllText(ErrorLogPath, log);
            }
            catch
            {
            }
        }
    }
}

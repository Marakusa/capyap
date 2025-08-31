using Microsoft.Win32;

namespace CapYap.Utils.Windows
{
    public static class StartupUtils
    {
        private const string RUN_KEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "CapYap";

        public static bool IsStartupEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
            var value = key?.GetValue(APP_NAME) as string;
            return !string.IsNullOrEmpty(value);
        }

        public static void EnableStartup()
        {
            string? exePath = Environment.ProcessPath;

            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            key?.SetValue(APP_NAME, $"\"{exePath}\"");
        }

        public static void DisableStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            key?.DeleteValue(APP_NAME, false);
        }
    }
}

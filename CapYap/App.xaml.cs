using CapYap.API;
using CapYap.HotKeys.Windows;
using CapYap.Interfaces;
using CapYap.Services;
using CapYap.Settings;


#if !DEBUG
using CapYap.Updater;
#endif
using CapYap.Utils.Windows;
using CapYap.ViewModels.Pages;
using CapYap.ViewModels.Windows;
using CapYap.Views.Pages;
using CapYap.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;

namespace CapYap
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        public static string ApiHost { get; } = "https://capyap.marakusa.me";
        public static string WebSiteHost { get; } = "https://capyap.marakusa.me";
        public static string Version { get; private set; } = "";

        // Windows API imports to focus the existing window
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        static App()
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "Logs", "app.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true)
                .WriteTo.File("log.txt", LogEventLevel.Information)
                .CreateLogger();
        }

        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c =>
            {
                c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)!);
                c.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                c.AddEnvironmentVariables();
            })
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                var logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(context.Configuration)
                    .CreateLogger();

                services.AddSerilog(logger);

                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow, MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                // Login window
                services.AddSingleton<LoginWindow>();
                services.AddSingleton<LoginWindowViewModel>();

                services.AddScoped<HttpClient>();

                services.AddSingleton<CapYapApi>();
                services.AddSingleton<HotKeyManager>();

                services.AddSingleton<IApiService, ApiService>();
                services.AddSingleton<IImageCacheService, ImageCacheService>();
                services.AddSingleton<IScreenshotService, ScreenshotService>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<DataPage>();
                services.AddSingleton<DataViewModel>();
                services.AddSingleton<UploadPage>();
                services.AddSingleton<UploadViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();

                services.AddSingleton<AudioUtils>();
            }).Build();

        public static IServiceProvider Services => _host.Services;

        public static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Unhandled exception occurred:\n{e.Exception.Message}\nStack trace:\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            Current.Dispatcher.UnhandledException += OnDispatcherUnhandledException;

            var log = _host.Services.GetService<ILogger>();

            if (IsAnotherInstanceRunning())
            {
                log?.Warning("Another instance running. Shutting down...");
                Shutdown(); // Exit this instance
                return;
            }

            if (UserSettingsManager.Current.AppSettings.AutoStart && !StartupUtils.IsStartupEnabled())
            {
                StartupUtils.EnableStartup();
            }
            else if (!UserSettingsManager.Current.AppSettings.AutoStart && StartupUtils.IsStartupEnabled())
            {
                StartupUtils.DisableStartup();
            }

#if DEBUG
            DateTime now = DateTime.Now;
            string year = now.Year.ToString().Substring(2);
            string month = now.Month.ToString("D1");
            string day = now.Day.ToString("D2");
            string hour = now.Hour.ToString("D1");
            string minute = now.Minute.ToString("D2");
            Version = $"{year}.{month}{day}.{hour}{minute}";
#else
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
            if (Version.Split('.').Length > 3)
            {
                string[] parts = Version.Split(".");
                Version = $"{parts[0]}.{parts[1]}.{parts[2]}";
            }
#endif

#if !DEBUG
            // Check for updates
            var updater = new AutoUpdater(log, "Marakusa", "capyap", Version);
            bool updated = await updater.CheckAndUpdateAsync();
            if (updated)
            {
                Shutdown();
                return;
            }
#endif

            log?.Information("App up to date! Starting...");

            await _host.StartAsync();
        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        private static bool IsAnotherInstanceRunning()
        {
            var current = Process.GetCurrentProcess();
            var other = Process.GetProcessesByName(current.ProcessName)
                .FirstOrDefault(p => p.Id != current.Id);

            if (other != null)
            {
                IntPtr handle = other.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    ShowWindow(handle, SW_RESTORE);
                    SetForegroundWindow(handle);
                }
                return true;
            }

            return false;
        }
    }
}

using CapYap.Properties;
using CapYap.Utils.Windows;
using CapYap.Views.Pages;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace CapYap.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = string.Empty;

        [ObservableProperty]
        private bool _autoStart = true;

        [ObservableProperty]
        private ApplicationTheme _currentTheme = ApplicationTheme.Unknown;

        public Task OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();

            return Task.CompletedTask;
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            AutoStart = StartupUtils.IsStartupEnabled();
            CurrentTheme = ApplicationThemeManager.GetAppTheme();

#if DEBUG
            AppVersion = $"CapYap - {App.Version} (DEBUG BUILD)";
#else
            AppVersion = $"CapYap - {App.Version}";
#endif

            _isInitialized = true;
        }

        [RelayCommand]
        private void OnChangeAutoStart(bool enabled)
        {
            AutoStart = enabled;

            if (AutoStart)
            {
                StartupUtils.EnableStartup();
            }
            else
            {
                StartupUtils.DisableStartup();
            }

            AppSettings.Default.AutoStart = AutoStart;
            AppSettings.Default.Save();
        }

        [RelayCommand]
        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;

                    break;

                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        break;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;

                    break;
            }
        }
    }
}

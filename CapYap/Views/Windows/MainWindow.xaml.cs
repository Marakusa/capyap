using System.ComponentModel;
using System.Windows.Controls;
using CapYap.API.Models.Appwrite;
using CapYap.HotKeys;
using CapYap.HotKeys.Models;
using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.Utils;
using CapYap.ViewModels.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CapYap.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        private readonly IAuthorizationService _authService;
        private readonly IScreenshotService _screenshotService;

        private readonly LoginWindow _loginWindow;

        private User? _currentUser;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IAuthorizationService authorizationService,
            LoginWindow loginWindow,
            IScreenshotService screenshotService,
            HotKeyManager hotKeys
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            _authService = authorizationService;
            _screenshotService = screenshotService;

            RegisterHotkeys(hotKeys);

            _loginWindow = loginWindow;

            _authService.OnUserChanged += OnUserChanged;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);
        }

        private void RegisterHotkeys(HotKeyManager hotKeyManager)
        {
            hotKeyManager.HotKey_ScreenCapture += CaptureScreen;

            hotKeyManager.Bind(BindingAction.CaptureScreen, System.Windows.Input.Key.PrintScreen, KeyModifier.Ctrl);
            hotKeyManager.Bind(BindingAction.CloseCropView, System.Windows.Input.Key.Escape, KeyModifier.None);
        }

        private void CaptureScreen(HotKey key)
        {
            if (_currentUser == null)
            {
                return;
            }

            _screenshotService.CaptureAllScreens();
        }

        private void OnUserChanged(object? sender, User? user)
        {
            if (user == null)
            {
                Hide();
                _currentUser = null;
                _loginWindow.Owner = this;
                _loginWindow.ShowDialog();
                Show();
                return;
            }

            _currentUser = user;

            AccountButton.Content = _currentUser.Name;

            AccountButton.ContextMenu = new ContextMenu();

            Wpf.Ui.Controls.MenuItem external = new();
            external.Header = "Open in browser";
            external.Click += External_Click;
            AccountButton.ContextMenu.Items.Add(external);
            AccountButton.Click += AccountButton_Click;

            Separator separator = new Separator();
            AccountButton.ContextMenu.Items.Add(separator);

            Wpf.Ui.Controls.MenuItem logOut = new();
            logOut.Header = "Log Out";
            logOut.Click += LogOut_Click;
            AccountButton.ContextMenu.Items.Add(logOut);
            AccountButton.Click += AccountButton_Click;
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            AccountButton.ContextMenu.PlacementTarget = sender as UIElement;
            AccountButton.ContextMenu.IsOpen = true;
        }

        private void External_Click(object sender, RoutedEventArgs e)
        {
            AppUtils.OpenUrl("https://sc.marakusa.me/settings");
        }

        private void LogOut_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            _authService.LogOutAsync();
            _loginWindow?.ShowDialog();
            Show();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow()
        {
            Width = WindowSettings.Default.Width;
            Height = WindowSettings.Default.Height;

            if (WindowSettings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }

            Show();
            Hide();

            _loginWindow.Owner = this;
            _loginWindow.ShowDialog();

            Show();
        }

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

        protected override void OnClosing(CancelEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowSettings.Default.Width = RestoreBounds.Width;
                WindowSettings.Default.Height = RestoreBounds.Height;
                WindowSettings.Default.Maximized = true;
            }
            else
            {
                WindowSettings.Default.Width = Width;
                WindowSettings.Default.Height = Height;
                WindowSettings.Default.Maximized = false;
            }

            WindowSettings.Default.Save();

            base.OnClosing(e);
        }

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Make sure that closing this window will begin the process of closing the application.
            Application.Current.Shutdown();
        }

        INavigationView INavigationWindow.GetNavigation()
        {
            throw new NotImplementedException();
        }

        public void SetServiceProvider(IServiceProvider serviceProvider)
        {
            throw new NotImplementedException();
        }
    }
}

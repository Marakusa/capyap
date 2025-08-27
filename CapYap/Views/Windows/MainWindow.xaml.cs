using System.Windows.Controls;
using CapYap.API.Models.Appwrite;
using CapYap.Interfaces;
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
        private readonly LoginWindow _loginWindow;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IAuthorizationService authorizationService,
            LoginWindow loginWindow,
            IScreenshotService screenshotService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            _authService = authorizationService;
            _loginWindow = loginWindow;

            _authService.OnUserChanged += OnUserChanged;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            screenshotService.CaptureAllScreens();
        }

        private void OnUserChanged(object? sender, User? user)
        {
            if (user == null)
            {
                _loginWindow.Owner = this;
                _loginWindow.ShowDialog();
                return;
            }

            AccountButton.Content = user.Name;

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
            _authService.LogOutAsync();
            _loginWindow?.ShowDialog();
        }

        #region INavigationWindow methods

        public INavigationView GetNavigation() => RootNavigation;

        public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

        public void SetPageService(INavigationViewPageProvider navigationViewPageProvider) => RootNavigation.SetPageProviderService(navigationViewPageProvider);

        public void ShowWindow()
        {
            Show();
            _loginWindow.Owner = this;
            _loginWindow.ShowDialog();
        }

        public void CloseWindow() => Close();

        #endregion INavigationWindow methods

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

using CapYap.API.Models.Appwrite;
using CapYap.HotKeys;
using CapYap.HotKeys.Models;
using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.Utils;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Windows;
using CapYap.Views.Pages;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace CapYap.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        public MainWindowViewModel ViewModel { get; }

        private readonly HotKeyManager _hotKeys;

        private readonly IApiService _apiService;
        private readonly IScreenshotService _screenshotService;

        private readonly LoginWindow _loginWindow;

        private User? _currentUser;

        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IApiService apiService,
            LoginWindow loginWindow,
            IScreenshotService screenshotService,
            HotKeyManager hotKeys
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            _apiService = apiService;
            _screenshotService = screenshotService;

            _hotKeys = hotKeys;
            RegisterHotkeys(_hotKeys);

            _loginWindow = loginWindow;

            _apiService.OnUserChanged += OnUserChanged;

            SystemThemeWatcher.Watch(this);

            InitializeComponent();
            SetPageService(navigationViewPageProvider);

            navigationService.SetNavigationControl(RootNavigation);

            DataPage.ImageClicked += (_, url) =>
            {
                PreviewImage(url);
            };
        }

        private void RegisterHotkeys(HotKeyManager hotKeyManager)
        {
            hotKeyManager.HotKey_ScreenCapture += CaptureScreen;

            hotKeyManager.Bind(BindingAction.CaptureScreen, System.Windows.Input.Key.PrintScreen, KeyModifier.Ctrl);
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
            _apiService.LogOutAsync();
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

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            SaveButton.Click += SaveButtonClick;
            ShareButton.Click += ShareButtonClick;
            DeleteButton.Click += DeleteButtonClick;
            /* PreviewPanel.MouseUp += (object sender, MouseButtonEventArgs e) =>
            {
                if (e.Source is Wpf.Ui.Controls.Image)
                {
                    return;
                }

                if (e.Source is Grid grid && grid.Name != "PreviewPanel")
                {
                    return;
                }

                ClosePreviewView();
            };*/
            PreviewPanel.MouseWheel += PreviewPanelMouseWheel;
            PreviewPanel.MouseDown += PreviewPanelMouseDown;
            PreviewPanel.MouseMove += PreviewPanelMouseMove;
            PreviewPanel.MouseUp += PreviewPanelMouseUp;
        }

        private string? _currentPreviewImage;
        private BitmapImage? _currentPreviewImageBitmap;
        private double previewScale = 1;

        private void PreviewImage(string? url)
        {
            _currentPreviewImage = url;
            PreviewPanel.Visibility = url == null ? Visibility.Hidden : Visibility.Visible;

            if (url != null)
            {
                _currentPreviewImageBitmap = new BitmapImage();
                _currentPreviewImageBitmap.BeginInit();
                _currentPreviewImageBitmap.UriSource = new Uri(url);
                _currentPreviewImageBitmap.CacheOption = BitmapCacheOption.OnLoad;
                _currentPreviewImageBitmap.EndInit();
                _currentPreviewImageBitmap.DownloadCompleted += (s, e) => CenterImage(_currentPreviewImageBitmap);

                PreviewImageComponent.Source = _currentPreviewImageBitmap;
            }
        }

        private void CenterImage(BitmapImage bmp)
        {
            PreviewPanel.UpdateLayout();

            previewScale = Math.Min(Width / bmp.Width, Height / bmp.Height);
            if (previewScale > 1)
            {
                previewScale = 1;
            }

            // Reset zoom & translation
            PreviewImageScale.ScaleX = previewScale;
            PreviewImageScale.ScaleY = previewScale;
            PreviewImageTranslate.X = 0;
            PreviewImageTranslate.Y = 0;
            PreviewImageComponent.RenderTransformOrigin = new Point(0.5, 0.5);

            double panelWidth = PreviewPanel.ActualWidth;
            double panelHeight = PreviewPanel.ActualHeight;
            double imgWidth = bmp.PixelWidth;
            double imgHeight = bmp.PixelHeight;

            double left = (panelWidth - imgWidth) / 2;
            double top = (panelHeight - imgHeight) / 2;

            Canvas.SetLeft(PreviewImageComponent, left);
            Canvas.SetTop(PreviewImageComponent, top);
        }

        private void ClosePreviewView()
        {
            PreviewImage(null);
        }

        private void SaveButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void ShareButtonClick(object sender, RoutedEventArgs e)
        {
            string url = _currentPreviewImage ?? "";
            string noViewString = "&noView=1";
            if (url.EndsWith(noViewString))
            {
                url = url.Substring(0, url.Length - noViewString.Length);
            }
            ClipboardUtils.SetClipboard(url);
            new Toast.Toast().SetSuccess("Link copied to clipboard.");
        }

        private void DeleteButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void PreviewPanelMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (PreviewImageComponent.Source == null)
                return;

            // Position of mouse over the image
            Point mousePos = e.GetPosition(PreviewImageComponent);

            double relativeX = mousePos.X / PreviewImageComponent.ActualWidth;
            double relativeY = mousePos.Y / PreviewImageComponent.ActualHeight;

            relativeX = Math.Clamp(relativeX, -PreviewImageComponent.ActualWidth / 20.0, PreviewImageComponent.ActualWidth / 20.0);
            relativeY = Math.Clamp(relativeY, -PreviewImageComponent.ActualHeight / 20.0, PreviewImageComponent.ActualHeight / 20.0);

            //PreviewImageComponent.RenderTransformOrigin = new Point(relativeX, relativeY);

            double zoomStep = 0.05;
            double minZoom = 0.5 * previewScale;
            double maxZoom = 5.0;

            double scale = PreviewImageScale.ScaleX;
            scale += (e.Delta > 0) ? zoomStep : -zoomStep;
            scale = Math.Max(minZoom, Math.Min(maxZoom, scale));

            PreviewImageScale.ScaleX = scale;
            PreviewImageScale.ScaleY = scale;
        }

        bool _mouseDownPreview = false;
        Point _lastMousePos = new Point();

        private void PreviewPanelMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (PreviewImageComponent.Source == null)
                return;

            _lastMousePos = e.GetPosition(PreviewPanel);
            _mouseDownPreview = true;
            PreviewPanel.CaptureMouse(); // capture the mouse while dragging
        }

        private void PreviewPanelMouseUp(object sender, MouseButtonEventArgs e)
        {
            _mouseDownPreview = false;
            PreviewPanel.ReleaseMouseCapture();
        }

        private void PreviewPanelMouseMove(object sender, MouseEventArgs e)
        {
            if (!_mouseDownPreview)
                return;

            Point mousePos = e.GetPosition(PreviewPanel);
            Vector delta = mousePos - _lastMousePos;

            // Apply translation
            PreviewImageTranslate.X += delta.X;
            PreviewImageTranslate.Y += delta.Y;

            _lastMousePos = mousePos;
        }
    }
}

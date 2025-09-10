using CapYap.API.Models;
using CapYap.API.Models.Appwrite;
using CapYap.HotKeys;
using CapYap.HotKeys.Models;
using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.Tray;
using CapYap.Utils;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Windows;
using CapYap.Views.Pages;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Abstractions;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace CapYap.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private ILogger<MainWindow> _log;

        public MainWindowViewModel ViewModel { get; }

        private readonly HotKeyManager _hotKeys;

        private readonly IApiService _apiService;
        private readonly IImageCacheService _imageCache;
        private readonly IScreenshotService _screenshotService;

        private readonly LoginWindow _loginWindow;

        private User? _currentUser;

        private TrayIcon? _trayIcon;

        public MainWindow(
            ILogger<MainWindow> log,
            MainWindowViewModel viewModel,
            INavigationViewPageProvider navigationViewPageProvider,
            INavigationService navigationService,
            IApiService apiService,
            LoginWindow loginWindow,
            IScreenshotService screenshotService,
            HotKeyManager hotKeys,
            IImageCacheService imageCache
        )
        {
            _log = log;

            try
            {
                _log.LogInformation("Creating main window...");

                ViewModel = viewModel;
                DataContext = this;

                _apiService = apiService;
                _imageCache = imageCache;
                _screenshotService = screenshotService;

                _hotKeys = hotKeys;
                RegisterHotkeys(_hotKeys);

                _loginWindow = loginWindow;

                _apiService.OnUserChanged += OnUserChanged;

                SystemThemeWatcher.Watch(this);

                InitializeComponent();
                SetPageService(navigationViewPageProvider);

                navigationService.SetNavigationControl(RootNavigation);

                DataPage.ImageClicked += async (_, p) =>
                {
                    string url = p.Item1;
                    int views = p.Item2;
                    string size = p.Item3;
                    await PreviewImageAsync(url, views, size);
                };

                _trayIcon = new TrayIcon("CapYap", Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"), App.Version);
                _trayIcon.OnOpenClicked += (_, _) =>
                {
                    Show();
                    WindowState = WindowSettings.Default.Maximized ? WindowState.Maximized : WindowState.Normal;
                    Activate();
                    BringIntoView();
                };
                _trayIcon.OnCaptureClicked += (_, _) => _screenshotService.CaptureAllScreens();
                _trayIcon.OnOpenExternalClicked += (_, _) => OpenExternal();
                _trayIcon.OnExitClicked += (_, _) => Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to initialize main window: {ex}");
            }
        }

        private void RegisterHotkeys(HotKeyManager hotKeyManager)
        {
            hotKeyManager.HotKey_ScreenCapture += CaptureScreen;

            hotKeyManager.Bind(BindingAction.CaptureScreen, Key.PrintScreen, KeyModifier.Ctrl);
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
            OpenExternal();
        }
        private void OpenExternal()
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

            _loginWindow.Owner = this;
            _loginWindow.ShowDialog();

            Activate();
        }

        public void CloseWindow()
        {
            _log.LogInformation("CloseWindow called");
            Hide();
        }

        #endregion INavigationWindow methods

        protected override void OnClosing(CancelEventArgs e)
        {
            _log.LogInformation("Window closing invoked...");

            e.Cancel = true;
            Hide();

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

            _log.LogInformation("Window state saved...");

            //base.OnClosing(e);
        }

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _log.LogInformation("Window closed");

            base.OnClosed(e);

            _log.LogInformation("App shutting down...");

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
            SaveButton.Click += async (sender, e) =>
            {
                await SaveButtonClickAsync(sender, e);
            };
            ShareButton.Click += ShareButtonClick;
            DeleteButton.Click += async (sender, e) =>
            {
                await DeleteButtonClickAsync(sender, e);
            };
            PreviewPanel.MouseUp += (object sender, MouseButtonEventArgs e) =>
            {
                if (PreviewPanel.Visibility == Visibility.Hidden)
                {
                    return;
                }

                Point mousePos = e.GetPosition(PreviewImageComponent);
                if (PreviewImageComponent.RenderSize.Width > mousePos.X && 0 < mousePos.X &&
                    PreviewImageComponent.RenderSize.Height > mousePos.Y && 0 < mousePos.Y)
                {
                    return;
                }

                _ = PreviewImageAsync(null, 0, "0 B");
            };
            PreviewPanel.MouseWheel += PreviewPanelMouseWheel;
            PreviewPanel.MouseDown += PreviewPanelMouseDown;
            PreviewPanel.MouseMove += PreviewPanelMouseMove;
            PreviewPanel.MouseUp += PreviewPanelMouseUp;

            KeyUp += (object sender, System.Windows.Input.KeyEventArgs e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _ = PreviewImageAsync(null, 0, "0 B");
                }
            };
        }

        private string? _currentPreviewImage;
        private BitmapImage? _currentPreviewImageBitmap;
        private double previewScale = 1;

        private async Task PreviewImageAsync(string? url, int views, string size)
        {
            LoadingRing.Visibility = Visibility.Hidden;
            _currentPreviewImage = url;
            PreviewPanel.Visibility = url == null ? Visibility.Hidden : Visibility.Visible;

            if (url == null)
            {
                return;
            }

            // Reuse from cache
            var bmp = _imageCache.GetImage(url);
            _currentPreviewImageBitmap = new BitmapImage();
            _currentPreviewImageBitmap = bmp;
            _currentPreviewImageBitmap.DownloadProgress += (_, _) =>
            {
                LoadingRing.Visibility = Visibility.Visible;
            };
            _currentPreviewImageBitmap.DownloadCompleted += (_, _) =>
            {
                CenterImage(_currentPreviewImageBitmap);
                LoadingRing.Visibility = Visibility.Hidden;
            };

            PreviewImageComponent.Source = bmp;

            CenterImage(_currentPreviewImageBitmap);

            ViewsText.Text = views.ToString();
            SizeText.Text = size;

            string filePath = new Uri(url).AbsolutePath;
            if (filePath.StartsWith("/f/"))
            {
                filePath = filePath.Substring(3);
            }
            FileStats? fileStats = await _apiService.FetchFileStatsAsync(filePath);
            if (fileStats != null)
            {
                ViewsText.Text = fileStats.Views.ToString();
                SizeText.Text = fileStats.Size;
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
            previewScale *= 0.9;

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

        private async Task SaveButtonClickAsync(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Title = "Save",
                Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png",
                FileName = "image.jpg"
            };
            bool saveDialog = saveFileDialog.ShowDialog(this) ?? false;

            if (saveDialog)
            {
                Toast.Toast saveToast = new();
                try
                {
                    saveToast.SetWait("Saving cap...");
                    string saveFilePath = saveFileDialog.FileName;

                    using HttpClient client = new();
                    var imageBytes = await client.GetByteArrayAsync(_currentPreviewImage);

                    await File.WriteAllBytesAsync(saveFilePath, imageBytes);

                    saveToast.SetSuccess("Cap saved successfully!");
                }
                catch (Exception ex)
                {
                    _log.LogError($"Failed to save cap: {ex}");
                    saveToast.SetFail($"Failed to save cap: {ex.Message}");
                }
            }
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

        private async Task DeleteButtonClickAsync(object sender, RoutedEventArgs e)
        {
            var contentDialogService = new ContentDialogService();
            contentDialogService.SetDialogHost(RootContentDialogPresenter);

            SimpleContentDialogCreateOptions options = new()
            {
                Title = "Delete a cap",
                Content = "Are you sure you want to delete this cap? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel"
            };
            ContentDialogResult dialogResult = await contentDialogService.ShowSimpleDialogAsync(options);
            
            if (dialogResult != ContentDialogResult.Primary)
            {
                return;
            }

            Toast.Toast deleteToast = new();
            try
            {
                deleteToast.SetWait("Deleting cap...");

                await _apiService.DeleteCaptureAsync(_currentPreviewImage);

                deleteToast.SetSuccess("Cap deleted successfully!");

                await _apiService.FetchGalleryAsync();
                _ = PreviewImageAsync(null, 0, "0 B");
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to delete cap: {ex}");
                deleteToast.SetFail($"Failed to delete cap: {ex.Message}");
            }
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

        private void PreviewPanelMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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

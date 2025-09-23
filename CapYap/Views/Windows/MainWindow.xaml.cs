using CapYap.API.Models;
using CapYap.API.Models.Appwrite;
using CapYap.HotKeys.Windows;
using CapYap.HotKeys.Windows.Models;
using CapYap.Interfaces;
using CapYap.Settings;
using CapYap.Tray;
using CapYap.Utils;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Windows;
using CapYap.Views.Pages;
using Serilog;
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
using WpfAnimatedGif;

namespace CapYap.Views.Windows
{
    public partial class MainWindow : INavigationWindow
    {
        private ILogger _log;

        public MainWindowViewModel ViewModel { get; }

        private readonly HotKeyManager _hotKeys;

        private readonly IApiService _apiService;
        private readonly IImageCacheService _imageCache;
        private readonly IScreenshotService _screenshotService;

        private readonly LoginWindow _loginWindow;

        private User? _currentUser;

        private TrayIcon? _trayIcon;

        public MainWindow(
            ILogger log,
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
                _log.Information("Creating main window...");

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

                _trayIcon = new TrayIcon("CapYap", Path.Combine(AppContext.BaseDirectory, "Assets", "icon.ico"), App.Version
#if DEBUG
                + " (DEBUG BUILD)"
#endif
                );
                _trayIcon.OnOpenClicked += (_, _) =>
                {
                    Show();
                    WindowState = UserSettingsManager.Current.WindowSettings.Maximized ? WindowState.Maximized : WindowState.Normal;
                    Activate();
                    BringIntoView();
                };
                _trayIcon.OnCaptureClicked += (_, _) => _screenshotService.Capture();
                _trayIcon.OnOpenExternalClicked += (_, _) => OpenExternal();
                _trayIcon.OnExitClicked += (_, _) => Application.Current.Shutdown();

                if (UserSettingsManager.Current.AppSettings.Theme == "theme_light")
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                }
                else
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to initialize main window: {ex}");
            }
        }

        private void RegisterHotkeys(HotKeyManager hotKeyManager)
        {
            hotKeyManager.HotKey_ScreenCapture += CaptureScreen;

            hotKeyManager.Bind(BindingAction.CaptureScreen, SharpDX.DirectInput.Key.PrintScreen, KeyModifier.Ctrl);
        }

        private void CaptureScreen(HotKey key)
        {
            if (_currentUser == null)
            {
                _log.Error("User not logged in.");
                return;
            }

            _screenshotService.Capture();
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

            if (user.Prefs != null && user.Prefs.ContainsKey("photoURL") && !string.IsNullOrEmpty(user.Prefs?["photoURL"]))
            {
                UpdateAvatar(user.Prefs["photoURL"]);
            }

            AccountButton.ContextMenu = new ContextMenu();

            Wpf.Ui.Controls.MenuItem username = new();
            username.Header = _currentUser.Name;
            username.IsEnabled = false;
            AccountButton.ContextMenu.Items.Add(username);

            Wpf.Ui.Controls.MenuItem email = new();
            email.Header = _currentUser.Email;
            email.IsEnabled = false;
            AccountButton.ContextMenu.Items.Add(email);

            Separator separator = new Separator();
            AccountButton.ContextMenu.Items.Add(separator);

            Wpf.Ui.Controls.MenuItem external = new();
            external.Header = "Open in browser";
            external.Click += External_Click;
            AccountButton.ContextMenu.Items.Add(external);

            Separator separator2 = new Separator();
            AccountButton.ContextMenu.Items.Add(separator2);

            Wpf.Ui.Controls.MenuItem logOut = new();
            logOut.Header = "Log Out";
            logOut.Click += LogOut_Click;
            AccountButton.ContextMenu.Items.Add(logOut);

            Wpf.Ui.Controls.MenuItem quitApp = new();
            quitApp.Header = "Quit CapYap";
            quitApp.Click += (_, _) => Application.Current.Shutdown();
            AccountButton.ContextMenu.Items.Add(quitApp);

            AccountButton.MouseUp += AccountButton_Click;
        }

        private void UpdateAvatar(string url)
        {
            var bmp = _imageCache.GetImage(url);
            if (bmp != null)
            {
                if (bmp.IsDownloading)
                {
                    bmp.DownloadCompleted += (_, _) =>
                    {
                        AccountButton.Source = bmp;
                    };
                }
                else
                {
                    AccountButton.Source = bmp;
                }
            }
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
            AppUtils.OpenUrl(App.WebSiteHost + "/settings");
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
            Width = UserSettingsManager.Current.WindowSettings.Width;
            Height = UserSettingsManager.Current.WindowSettings.Height;

            if (UserSettingsManager.Current.WindowSettings.Maximized)
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
            _log.Information("CloseWindow called");
            Hide();
        }

        #endregion INavigationWindow methods

        protected override void OnClosing(CancelEventArgs e)
        {
            _log.Information("Window closing invoked...");

            e.Cancel = true;
            Hide();

            if (WindowState == WindowState.Maximized)
            {
                UserSettingsManager.Current.WindowSettings.Width = (int)RestoreBounds.Width;
                UserSettingsManager.Current.WindowSettings.Height = (int)RestoreBounds.Height;
                UserSettingsManager.Current.WindowSettings.Maximized = true;
            }
            else
            {
                UserSettingsManager.Current.WindowSettings.Width = (int)Width;
                UserSettingsManager.Current.WindowSettings.Height = (int)Height;
                UserSettingsManager.Current.WindowSettings.Maximized = false;
            }

            UserSettingsManager.Current.Save();

            _log.Information("Window state saved...");

            //base.OnClosing(e);
        }

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            _log.Information("Window closed");

            base.OnClosed(e);

            _log.Information("App shutting down...");

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
            SetAsAvatarButton.Click += async (sender, e) =>
            {
                await SetAsAvatarButtonClickAsync(sender, e);
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

            KeyUp += (object sender, KeyEventArgs e) =>
            {
                if (e.Key == Key.Escape)
                {
                    _ = PreviewImageAsync(null, 0, "0 B");
                }
            };
        }

        private string? _currentPreviewImage;
        private BitmapImage? _currentPreviewImageBitmap;
        private BitmapImage? _currentAnimatedPreviewImageBitmap;
        private double previewScale = 1;

        private async Task PreviewImageAsync(string? url, int views, string size)
        {
            LoadingRing.Visibility = Visibility.Hidden;
            _currentPreviewImage = url;
            PreviewPanel.Visibility = url == null ? Visibility.Hidden : Visibility.Visible;

            if (url == null) return;

            string extension = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
            bool isGif = extension == ".gif";

            // Reset previous image
            if (PreviewImageComponent.Source != null)
            {
                ImageBehavior.SetAnimatedSource(PreviewImageComponent, null);
                PreviewImageComponent.Source = null;
            }
            _currentAnimatedPreviewImageBitmap = null;
            _currentPreviewImageBitmap = null;

            if (isGif)
            {
                try
                {
                    // Use cache service for GIFs
                    _currentAnimatedPreviewImageBitmap = await _imageCache.GetGifImageAsync(url);

                    if (_currentAnimatedPreviewImageBitmap != null)
                    {
                        ImageBehavior.SetAnimatedSource(PreviewImageComponent, _currentAnimatedPreviewImageBitmap);
                        CenterImage();
                        LoadingRing.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        throw new Exception("Failed to load GIF from cache.");
                    }
                }
                catch (Exception ex)
                {
                    LoadingRing.Visibility = Visibility.Hidden;
                    new Toast.Toast().SetFail($"Failed to load GIF: {ex.Message}");
                }
            }
            else
            {
                try
                {
                    // Use cache service for static images
                    _currentPreviewImageBitmap = _imageCache.GetImage(url) ?? new BitmapImage(new Uri(url));

                    _currentPreviewImageBitmap.DownloadProgress += (_, _) => LoadingRing.Visibility = Visibility.Visible;
                    _currentPreviewImageBitmap.DownloadCompleted += (_, _) =>
                    {
                        CenterImage();
                        LoadingRing.Visibility = Visibility.Hidden;
                    };
                    _currentPreviewImageBitmap.DownloadFailed += (_, _) =>
                    {
                        LoadingRing.Visibility = Visibility.Hidden;
                        new Toast.Toast().SetFail("Failed to load image.");
                    };

                    ImageBehavior.SetAnimatedSource(PreviewImageComponent, null);
                    PreviewImageComponent.Source = _currentPreviewImageBitmap;
                    CenterImage();
                }
                catch
                {
                    LoadingRing.Visibility = Visibility.Hidden;
                }
            }

            ViewsText.Text = views.ToString();
            SizeText.Text = size;

            string filePath = new Uri(url).AbsolutePath;
            if (filePath.StartsWith("/f/"))
                filePath = filePath.Substring(3);

            FileStats? fileStats = await _apiService.FetchFileStatsAsync(filePath);
            if (fileStats != null)
            {
                ViewsText.Text = fileStats.Views.ToString();
                SizeText.Text = fileStats.Size;
            }
        }

        private void CenterImage()
        {
            PreviewPanel.UpdateLayout();

            BitmapSource? bmp = null;

            // Check if it's a static image
            if (PreviewImageComponent.Source is BitmapSource staticBmp)
            {
                bmp = staticBmp;
            }
            // Check if we have an animated GIF loaded
            else if (_currentAnimatedPreviewImageBitmap != null)
            {
                bmp = _currentAnimatedPreviewImageBitmap;
            }

            if (bmp == null || bmp.PixelWidth == 0 || bmp.PixelHeight == 0)
            {
                return;
            }

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
                    _log.Error($"Failed to save cap: {ex}");
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
                _log.Error($"Failed to delete cap: {ex}");
                deleteToast.SetFail($"Failed to delete cap: {ex.Message}");
            }
        }

        private Task SetAsAvatarButtonClickAsync(object sender, RoutedEventArgs e)
        {
            if (_currentUser == null || _currentPreviewImage == null)
            {
                new Toast.Toast().SetFail("You must be logged in and have a cap selected to set as avatar.");
                return Task.CompletedTask;
            }
            Toast.Toast avatarToast = new();
            try
            {
                avatarToast.SetWait("Setting avatar...");
                _apiService.UpdateAvatarAsync(_currentPreviewImage);
                avatarToast.SetSuccess("Avatar set successfully!");
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to set avatar: {ex}");
                avatarToast.SetFail($"Failed to set avatar: {ex.Message}");
            }
            return Task.CompletedTask;
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

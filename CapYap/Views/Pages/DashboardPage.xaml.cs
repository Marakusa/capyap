using CapYap.API;
using CapYap.API.Models;
using CapYap.Interfaces;
using CapYap.Models;
using CapYap.Properties;
using CapYap.ViewModels.Pages;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;
using WpfAnimatedGif;

namespace CapYap.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private readonly IApiService _apiService;
        private readonly IImageCacheService _imageCache;

        public static event EventHandler<(string, int, string)>? ImageClicked;

        public DashboardPage(
            DashboardViewModel viewModel,
            IApiService apiService,
            IImageCacheService imageCache
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            _apiService = apiService;
            _imageCache = imageCache;

            InitializeComponent();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            _apiService.OnStatsChanged += OnStatsChanged;
            _apiService.OnStatsFetching += OnStatsFetching;

            _apiService.OnGalleryChanged += OnGalleryChanged;
            _apiService.OnGalleryFetching += OnGalleryFetching;

            CapYapApi.ImageUploaded += (_, _) =>
            {
                _ = _apiService.FetchStatsAsync();
            };

            _ = _apiService.FetchStatsAsync();

            SettingsPage.ShowRecentFilesDashboardChanged += () =>
            {
                if (AppSettings.Default.ShowRecentFilesDashboard)
                {
                    RecentFiles.Visibility = Visibility.Visible;
                    RecentFilesTitle.Visibility = Visibility.Visible;
                    KeyGuide.Visibility = Visibility.Hidden;
                }
                else
                {
                    RecentFiles.Visibility = Visibility.Hidden;
                    RecentFilesTitle.Visibility = Visibility.Hidden;
                    KeyGuide.Visibility = Visibility.Visible;
                }
            };

            CapYapApi.ImageUploaded += (_, _) =>
            {
                LoadingRing.Visibility = Visibility.Visible;
                ErrorText.Visibility = Visibility.Hidden;
                ErrorText.Text = "";
                _ = _apiService.FetchGalleryAsync(1, 3);
            };
            RecentFiles.Visibility = Visibility.Hidden;
            RecentFilesTitle.Visibility = Visibility.Hidden;
            KeyGuide.Visibility = Visibility.Visible;
            LoadingRing.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";

            _ = _apiService.FetchGalleryAsync(1, 3);
        }

        private void OnStatsChanged(object? sender, Models.StatsChangedEventArgs e)
        {
            SetStats(e.Stats, false);
        }

        private void OnStatsFetching(EventArgs obj)
        {
            SetStats(null, true);
        }

        private void SetStats(Stats? stats, bool loading)
        {
            if (stats == null)
            {
                DiskUsed.Text = "0 KB";
                TotalViews.Text = "0";
                Files.Text = "0";
                Files7Days.Text = "0";
            }
            else
            {
                DiskUsed.Text = stats.SpaceUsed;
                TotalViews.Text = stats.Views.ToString();
                Files.Text = stats.TotalFiles.ToString();
                Files7Days.Text = stats.Files7Days.ToString();
            }

            DiskUsedLoading.Visibility = loading ? Visibility.Visible : Visibility.Hidden;
            TotalViewsLoading.Visibility = loading ? Visibility.Visible : Visibility.Hidden;
            FilesLoading.Visibility = loading ? Visibility.Visible : Visibility.Hidden;
            Files7DaysLoading.Visibility = loading ? Visibility.Visible : Visibility.Hidden;

            DiskUsed.Opacity = loading ? 0.5 : 1;
            TotalViews.Opacity = loading ? 0.5 : 1;
            Files.Opacity = loading ? 0.5 : 1;
            Files7Days.Opacity = loading ? 0.5 : 1;
        }

        private void OnGalleryChanged(object? sender, GalleryChangedEventArgs e)
        {
            if (e.Gallery?.Documents == null || e.Gallery.Documents.Count == 0)
            {
                GalleryControl.ItemsSource = null;
                if (e.Error)
                {
                    if (AppSettings.Default.ShowRecentFilesDashboard)
                    {
                        RecentFiles.Visibility = Visibility.Visible;
                        RecentFilesTitle.Visibility = Visibility.Visible;
                        KeyGuide.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        KeyGuide.Visibility = Visibility.Visible;
                    }
                    ErrorText.Visibility = Visibility.Visible;
                    LoadingRing.Visibility = Visibility.Hidden;
                    ErrorText.Text = e.ErrorMessage;
                }
                else
                {
                    RecentFiles.Visibility = Visibility.Hidden;
                    RecentFilesTitle.Visibility = Visibility.Hidden;
                    KeyGuide.Visibility = Visibility.Visible;
                    ErrorText.Visibility = Visibility.Hidden;
                    LoadingRing.Visibility = Visibility.Hidden;
                    ErrorText.Text = "";
                }
                return;
            }

            // Bind the list of URLs to the gallery
            GalleryControl.ItemsSource = e.Gallery.Documents.Select(d => d.ToString() + "&noView=1").ToList();

            if (AppSettings.Default.ShowRecentFilesDashboard)
            {
                RecentFiles.Visibility = Visibility.Visible;
                RecentFilesTitle.Visibility = Visibility.Visible;
                KeyGuide.Visibility = Visibility.Hidden;
            }
            else
            {
                KeyGuide.Visibility = Visibility.Visible;
            }
            LoadingRing.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
        }

        private void OnGalleryFetching(EventArgs e)
        {
            GalleryControl.ItemsSource = null;
            if (AppSettings.Default.ShowRecentFilesDashboard)
            {
                RecentFiles.Visibility = Visibility.Visible;
            }
            RecentFilesTitle.Visibility = Visibility.Hidden;
            KeyGuide.Visibility = Visibility.Hidden;
            LoadingRing.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";    
        }

        private void OnImageClick(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.Image img;
            if (sender is System.Windows.Controls.Image)
            {
                img = (System.Windows.Controls.Image)sender;
            }
            else
            {
                return;
            }

            string? url = img.DataContext.ToString();
            if (img.DataContext == null || string.IsNullOrEmpty(url))
            {
                return;
            }

            ImageClicked?.Invoke(this, (url, 0, "0 B"));
        }

        private async void OnGalleryImageLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Image img || img.DataContext is not string url)
                return;

            string extension = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
            bool isGif = extension == ".gif";

            if (isGif)
            {
                var gif = await _imageCache.GetGifImageAsync(url);
                if (gif != null)
                {
                    ImageBehavior.SetAnimatedSource(img, gif);
                }
                else
                {
                    img.Source = null;
                }
            }
            else
            {
                var bmp = _imageCache.GetImage(url) ?? new BitmapImage(new Uri(url));
                img.Source = bmp;
            }
        }
    }
}

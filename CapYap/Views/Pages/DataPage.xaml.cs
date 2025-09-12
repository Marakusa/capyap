using CapYap.API;
using CapYap.Interfaces;
using CapYap.Models;
using CapYap.ViewModels.Pages;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;
using WpfAnimatedGif;

namespace CapYap.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        private readonly IApiService _apiService;
        private readonly IImageCacheService _imageCache;

        public static event EventHandler<(string, int, string)>? ImageClicked;

        public DataPage(DataViewModel viewModel,
            IApiService apiService,
            IImageCacheService imageCache)
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

            _apiService.OnGalleryChanged += OnGalleryChanged;
            _apiService.OnGalleryFetching += OnGalleryFetching;

            CapYapApi.ImageUploaded += (_, _) =>
            {
                LoadingRing.Visibility = Visibility.Visible;
                ErrorText.Visibility = Visibility.Hidden;
                ErrorText.Text = "";
                _ = _apiService.FetchGalleryAsync();
            };
            LoadingRing.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
            _ = _apiService.FetchGalleryAsync();
        }

        private void OnGalleryChanged(object? sender, GalleryChangedEventArgs e)
        {
            if (e.Gallery != null)
            {
                SetPageButtons(e.Gallery.TotalPages, e.Gallery.Page);
            }
            else
            {
                SetPageButtons(1, 1);
            }

            if (e.Gallery?.Documents == null || e.Gallery.Documents.Count == 0)
            {
                GalleryControl.ItemsSource = null;
                if (e.Error)
                {
                    ErrorText.Visibility = Visibility.Visible;
                    LoadingRing.Visibility = Visibility.Hidden;
                    ErrorText.Text = e.ErrorMessage;
                }
                else
                {
                    ErrorText.Visibility = Visibility.Hidden;
                    LoadingRing.Visibility = Visibility.Hidden;
                    ErrorText.Text = "";
                }
                return;
            }

            // Bind the list of URLs to the gallery
            GalleryControl.ItemsSource = e.Gallery.Documents.Select(d => d.ToString() + "&noView=1").ToList();

            LoadingRing.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
        }

        private void OnGalleryFetching(EventArgs e)
        {
            GalleryControl.ItemsSource = null;
            LoadingRing.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
            PageBar.IsEnabled = false;
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

        #region Page actions
        private void OnPrevButton()
        {
            _ = _apiService.FetchGalleryPrevAsync();
        }
        private void OnNextButton()
        {
            _ = _apiService.FetchGalleryNextAsync();
        }
        private void OnPageButton(object sender, EventArgs e)
        {
            int page = 1;
            if (!int.TryParse(((Wpf.Ui.Controls.Button)sender).Content.ToString(), out page))
            {
                page = 1;
            }
            _ = _apiService.FetchGalleryAsync(page);
        }
        #endregion

        #region Components
        private Wpf.Ui.Controls.Button UiPrevButton(bool active)
        {
            Wpf.Ui.Controls.Button button = new();
            button.Margin = new Thickness(2);
            button.Content = "<";
            button.Click += (_, _) => OnPrevButton();
            button.IsEnabled = active;
            return button;
        }
        private Wpf.Ui.Controls.Button UiNextButton(bool active)
        {
            Wpf.Ui.Controls.Button button = new();
            button.Margin = new Thickness(2);
            button.Content = ">";
            button.Click += (_, _) => OnNextButton();
            button.IsEnabled = active;
            return button;
        }
        private Wpf.Ui.Controls.Button UiPageButton(int page, bool currentPage)
        {
            Wpf.Ui.Controls.Button button = new();
            button.Margin = new Thickness(2);
            button.Content = page;
            button.Click += OnPageButton;
            if (currentPage)
            {
                button.Background = (SolidColorBrush?)new BrushConverter().ConvertFrom("#5E5EFF");
            }
            return button;
        }
        private Wpf.Ui.Controls.TextBlock UiMiddleDots()
        {
            Wpf.Ui.Controls.TextBlock text = new();
            text.TextWrapping = TextWrapping.NoWrap;
            text.Text = "...";
            text.Margin = new Thickness(4, 4, 4, 0);
            text.FontSize = 22;
            return text;
        }
        #endregion

        private void SetPageButtons(int totalPages, int currentPage)
        {
            PageBar.Children.Clear();

            PageBar.Children.Add(UiPrevButton(currentPage > 1));

            int pagesShown = 6; // Number of page buttons to show around the current page

            for (int i = 1; i <= totalPages; i++)
            {
                // If there are fewer pages, show all page buttons
                if (totalPages <= pagesShown)
                {
                    PageBar.Children.Add(UiPageButton(i, i == currentPage));
                    continue;
                }

                // If the current page is 1–4, show the first 6 pages and the last page
                if (currentPage <= 4)
                {
                    if (i <= pagesShown)
                    {
                        PageBar.Children.Add(UiPageButton(i, i == currentPage));
                    }
                    if (i == pagesShown + 1)
                    {
                        PageBar.Children.Add(UiMiddleDots());
                    }
                    if (i == totalPages)
                    {
                        PageBar.Children.Add(UiPageButton(totalPages, i == currentPage));
                    }
                    continue;
                }

                // If the current page is in the middle
                if (currentPage > 4 && currentPage < totalPages - 3)
                {
                    if (i == 1)
                    {
                        PageBar.Children.Add(UiPageButton(1, i == currentPage));
                        PageBar.Children.Add(UiMiddleDots());
                        continue;
                    }
                    if (i > currentPage - 3 && i < currentPage + 3)
                    {
                        PageBar.Children.Add(UiPageButton(i, i == currentPage));
                        continue;
                    }
                    if (i == totalPages)
                    {
                        PageBar.Children.Add(UiMiddleDots());
                        PageBar.Children.Add(UiPageButton(totalPages, i == currentPage));
                        continue;
                    }
                }

                // If the current page is near the end
                if (currentPage >= totalPages - 3)
                {
                    if (i == 1)
                    {
                        PageBar.Children.Add(UiPageButton(1, i == currentPage));
                        PageBar.Children.Add(UiMiddleDots());
                        continue;
                    }
                    if (i >= totalPages - pagesShown + 1)
                    {
                        PageBar.Children.Add(UiPageButton(i, i == currentPage));
                        continue;
                    }
                }
            }

            PageBar.Children.Add(UiNextButton(currentPage < totalPages));
            PageBar.IsEnabled = true;
        }
    }
}

using System.Windows.Media;
using CapYap.Interfaces;
using CapYap.Models;
using CapYap.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        private readonly IApiService _apiService;

        public DataPage(DataViewModel viewModel,
            IApiService apiService)
        {
            ViewModel = viewModel;
            DataContext = this;

            _apiService = apiService;

            _apiService.OnGalleryChanged += OnGalleryChanged;
            _apiService.OnGalleryFetching += OnGalleryFetching;

            InitializeComponent();

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
            GalleryControl.ItemsSource = e.Gallery.Documents;
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
                button.Background = Brushes.MediumPurple;
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

            for (int i = 1; i <= totalPages; i++)
            {
                if (i <= currentPage - 2)
                {
                    if (i == 2 && i <= totalPages - 8)
                    {
                        PageBar.Children.Add(UiPageButton(1, i == currentPage));
                        PageBar.Children.Add(UiMiddleDots());
                    }
                    else if (i > totalPages - 8)
                    {
                        PageBar.Children.Add(UiPageButton(i, i == currentPage));
                    }
                }
                if (i > currentPage - 2 && i < currentPage + 7)
                {
                    PageBar.Children.Add(UiPageButton(i, i == currentPage));
                }
                if (i == currentPage + 7)
                {
                    if (i != totalPages)
                    {
                        PageBar.Children.Add(UiMiddleDots());
                    }
                    PageBar.Children.Add(UiPageButton(totalPages, i == currentPage));
                    break;
                }
            }

            PageBar.Children.Add(UiNextButton(currentPage < totalPages));
            PageBar.IsEnabled = true;
        }
    }
}

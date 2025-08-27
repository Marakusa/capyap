using CapYap.Interfaces;
using CapYap.Models;
using CapYap.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class DataPage : INavigableView<DataViewModel>
    {
        public DataViewModel ViewModel { get; }

        private readonly IAuthorizationService _authService;

        public DataPage(DataViewModel viewModel,
            IAuthorizationService authorizationService)
        {
            ViewModel = viewModel;
            DataContext = this;

            _authService = authorizationService;

            _authService.OnGalleryChanged += OnGalleryChanged;

            InitializeComponent();

            LoadingRing.Visibility = Visibility.Visible;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
            _ = _authService.FetchGalleryAsync();
        }

        private void OnGalleryChanged(object? sender, GalleryChangedEventArgs e)
        {
            if (e.Gallery == null || e.Gallery.Count == 0)
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
            GalleryControl.ItemsSource = e.Gallery;
            LoadingRing.Visibility = Visibility.Hidden;
            ErrorText.Visibility = Visibility.Hidden;
            ErrorText.Text = "";
        }
    }
}

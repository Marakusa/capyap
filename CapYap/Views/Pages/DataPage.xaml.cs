using CapYap.Interfaces;
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
            _ = _authService.FetchGalleryAsync();
        }

        private void OnGalleryChanged(object? sender, List<string>? imageUrls)
        {
            if (imageUrls == null || imageUrls.Count == 0)
            {
                GalleryControl.ItemsSource = null;
                return;
            }

            // Bind the list of URLs to the gallery
            GalleryControl.ItemsSource = imageUrls;
            LoadingRing.Visibility = Visibility.Hidden;
        }
    }
}

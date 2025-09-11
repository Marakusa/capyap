using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Pages;
using Microsoft.Win32;
using System.IO;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;
using WpfAnimatedGif;

namespace CapYap.Views.Pages
{
    public partial class UploadPage : INavigableView<UploadViewModel>
    {
        public UploadViewModel ViewModel { get; }

        private IApiService _apiService;
        private readonly IImageCacheService _imageCache;

        public UploadPage(
            UploadViewModel viewModel,
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

            UploadSuccess.Visibility = Visibility.Hidden;
            UploadFail.Visibility = Visibility.Hidden;
            UploadingStatus.Visibility = Visibility.Hidden;
            UploadedImage.Visibility = Visibility.Hidden;
            UploadButton.IsEnabled = true;
        }

        private async Task OnUploadClicked()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Upload a screenshot",
                Multiselect = false
            };
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif";

            bool? fileSelected = openFileDialog.ShowDialog();
            if (fileSelected != null && fileSelected == true)
            {
                Toast.Toast toast = new();
                toast.SetWait("Uploading screen capture...");
                UploadingStatus.Visibility = Visibility.Visible;
                UploadSuccess.Visibility = Visibility.Hidden;
                UploadFail.Visibility = Visibility.Hidden;
                UploadedImage.Visibility = Visibility.Hidden;
                UploadButton.IsEnabled = false;
                try
                {
                    string file = openFileDialog.FileName;
                    string url = await _apiService.UploadCaptureAsync(file, AppSettings.Default.CompressionQuality, AppSettings.Default.CompressionLevel);
                    ClipboardUtils.SetClipboard(url);
                    toast.SetSuccess("Screen capture uploaded and copied to clipboard");

                    string extension = Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant();
                    bool isGif = extension == ".gif";

                    if (isGif)
                    {
                        var gif = await _imageCache.GetGifImageAsync(url);
                        if (gif != null)
                        {
                            ImageBehavior.SetAnimatedSource(UploadedImage, gif);
                        }
                        else
                        {
                            UploadedImage.Source = null;
                        }
                    }
                    else
                    {
                        var bmp = _imageCache.GetImage(url) ?? new BitmapImage(new Uri(url));
                        UploadedImage.Source = bmp;
                    }

                    UploadingStatus.Visibility = Visibility.Hidden;
                    UploadSuccess.Visibility = Visibility.Visible;
                    UploadFail.Visibility = Visibility.Hidden;
                    UploadedImage.Visibility = Visibility.Visible;
                    UploadButton.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    toast.SetFail(ex.Message);
                    UploadingStatus.Visibility = Visibility.Hidden;
                    UploadSuccess.Visibility = Visibility.Hidden;
                    UploadFail.Visibility = Visibility.Visible;
                    UploadedImage.Visibility = Visibility.Hidden;
                    UploadFail.Text = ex.Message;
                    UploadButton.IsEnabled = true;
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _ = OnUploadClicked();
        }
    }
}

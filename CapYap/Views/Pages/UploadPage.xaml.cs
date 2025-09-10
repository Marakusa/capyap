using CapYap.Interfaces;
using CapYap.Properties;
using CapYap.Utils.Windows;
using CapYap.ViewModels.Pages;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class UploadPage : INavigableView<UploadViewModel>
    {
        public UploadViewModel ViewModel { get; }

        private IApiService _apiService;

        public UploadPage(
            UploadViewModel viewModel,
            IApiService apiService
        )
        {
            ViewModel = viewModel;
            DataContext = this;

            _apiService = apiService;

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
            openFileDialog.Filter = "Image files (*.jpg, *.jpeg, *.png)|*.jpg;*.jpeg;*.png";

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
                    UploadingStatus.Visibility = Visibility.Hidden;
                    UploadSuccess.Visibility = Visibility.Visible;
                    UploadFail.Visibility = Visibility.Hidden;
                    UploadedImage.Visibility = Visibility.Visible;
                    UploadedImage.Source = new BitmapImage(new Uri(url));
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

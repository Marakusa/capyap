using CapYap.Settings;
using CapYap.ViewModels.Pages;
using System.Windows.Controls;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        private string _selectedFormat = "jpg";

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();

            // Load settings
            OnCompressionQualityChanged(UserSettingsManager.Current.UploadSettings.CompressionQuality);
            OnAnimCompressionQualityChanged(UserSettingsManager.Current.UploadSettings.AnimCompressionQuality);
            OnCompressionLevelChanged(UserSettingsManager.Current.UploadSettings.CompressionLevel);
            FormatComboBox.SelectedIndex = UserSettingsManager.Current.UploadSettings.UploadFormat;

            QuitButton.Click += (_, _) => Application.Current.Shutdown();

            FormatComboBox.SelectionChanged += (_, _) => OnFormatChanged();

            CompressionQualityNumber.ValueChanged += (_, _) => OnCompressionQualityChanged(CompressionQualityNumber.Value);
            CompressionQualitySlider.ValueChanged += (_, _) => OnCompressionQualityChanged(CompressionQualitySlider.Value);

            AnimCompressionQualityNumber.ValueChanged += (_, _) => OnAnimCompressionQualityChanged(AnimCompressionQualityNumber.Value);
            AnimCompressionQualitySlider.ValueChanged += (_, _) => OnAnimCompressionQualityChanged(AnimCompressionQualitySlider.Value);

            CompressionLevelNumber.ValueChanged += (_, _) => OnCompressionLevelChanged(CompressionLevelNumber.Value);
            CompressionLevelSlider.ValueChanged += (_, _) => OnCompressionLevelChanged(CompressionLevelSlider.Value);

            // Initial UI state
            OnFormatChanged();
        }

        private void OnCompressionQualityChanged(double? value)
        {
            int roundedValue = (int)Math.Round((decimal)(value ?? 92));
            CompressionQualityNumber.Value = roundedValue;
            CompressionQualitySlider.Value = roundedValue;
            UserSettingsManager.Current.UploadSettings.CompressionQuality = roundedValue;
            UserSettingsManager.Current.Save();
        }

        private void OnAnimCompressionQualityChanged(double? value)
        {
            int roundedValue = (int)Math.Round((decimal)(value ?? 70));
            AnimCompressionQualityNumber.Value = roundedValue;
            AnimCompressionQualitySlider.Value = roundedValue;
            UserSettingsManager.Current.UploadSettings.AnimCompressionQuality = roundedValue;
            UserSettingsManager.Current.Save();
        }

        private void OnCompressionLevelChanged(double? value)
        {
            int roundedValue = (int)Math.Round((decimal)(value ?? 6));
            CompressionLevelNumber.Value = roundedValue;
            CompressionLevelSlider.Value = roundedValue;
            UserSettingsManager.Current.UploadSettings.CompressionLevel = roundedValue;
            UserSettingsManager.Current.Save();
        }

        private void OnFormatChanged()
        {
            if (FormatComboBox.SelectedItem is not ComboBoxItem item)
            {
                return;
            }

            _selectedFormat = (item.Content.ToString() ?? "jpg").ToLower();

            switch (_selectedFormat)
            {
                default:
                case "jpg":
                    CompressionQualityLabel.IsEnabled = true;
                    CompressionQualityNumber.IsEnabled = true;
                    CompressionQualitySlider.IsEnabled = true;
                    CompressionLevelLabel.IsEnabled = false;
                    CompressionLevelNumber.IsEnabled = false;
                    CompressionLevelSlider.IsEnabled = false;
                    break;
                case "png":
                    CompressionQualityLabel.IsEnabled = true;
                    CompressionQualityNumber.IsEnabled = true;
                    CompressionQualitySlider.IsEnabled = true;
                    CompressionLevelLabel.IsEnabled = true;
                    CompressionLevelNumber.IsEnabled = true;
                    CompressionLevelSlider.IsEnabled = true;
                    break;
                case "gif":
                    CompressionQualityLabel.IsEnabled = false;
                    CompressionQualityNumber.IsEnabled = false;
                    CompressionQualitySlider.IsEnabled = false;
                    CompressionLevelLabel.IsEnabled = false;
                    CompressionLevelNumber.IsEnabled = false;
                    CompressionLevelSlider.IsEnabled = false;
                    break;
            }

            UserSettingsManager.Current.UploadSettings.UploadFormat = FormatComboBox.SelectedIndex;
            UserSettingsManager.Current.Save();
        }
    }
}

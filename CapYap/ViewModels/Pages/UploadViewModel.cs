using Microsoft.Win32;

namespace CapYap.ViewModels.Pages
{
    public partial class UploadViewModel : ObservableObject
    {
        [RelayCommand]
        private void OnUploadClicked()
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
                string file = openFileDialog.FileName;
            }
        }
    }
}

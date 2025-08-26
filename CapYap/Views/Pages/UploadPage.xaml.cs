using CapYap.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class UploadPage : INavigableView<UploadViewModel>
    {
        public UploadViewModel ViewModel { get; }

        public UploadPage(UploadViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
    }
}

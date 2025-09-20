using CapYap.API;
using CapYap.API.Models;
using CapYap.Interfaces;
using CapYap.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace CapYap.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        private readonly IApiService _apiService;

        public DashboardPage(
            DashboardViewModel viewModel,
            IApiService apiService,
            IImageCacheService imageCache
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

            _apiService.OnStatsChanged += OnStatsChanged;
            _apiService.OnStatsFetching += OnStatsFetching;

            CapYapApi.ImageUploaded += (_, _) =>
            {
                _ = _apiService.FetchStatsAsync();
            };

            _ = _apiService.FetchStatsAsync();
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
    }
}

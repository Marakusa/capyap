using CapYap.API.Models.Events;
using CapYap.Interfaces;
using CapYap.ViewModels.Windows;
using Serilog;

namespace CapYap.Views.Windows
{
    public partial class LoginWindow
    {
        private readonly ILogger _log;

        public LoginWindowViewModel ViewModel { get; }

        private readonly IApiService _apiService;

        private bool _authorized = false;
        private bool _authorizationInProgress = false;
        private string? _authFailMessage = null;
        private bool _authInitByUser = false;

        public event EventHandler<AuthorizedUserEventArgs?>? OnAuthorizedUser;

        public LoginWindow(
            ILogger log,
            LoginWindowViewModel viewModel,
            IApiService authorizationService
        )
        {
            _log = log;

            ViewModel = viewModel;
            _apiService = authorizationService;

            try
            {
                _log.Information("Creating login window...");

                InitializeComponent();
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to initialize login window: {ex}");
            }
        }

        /// <summary>
        /// Raises the closed event.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // If the user closes the window before the client has been authorized
            Application.Current.Shutdown();
        }

        private void OnClientAuthorized(object? sender, AuthorizedUserEventArgs e)
        {
            _authorized = true;
            _authorizationInProgress = false;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingText.Text = "Loading...";
            ErrorText.Visibility = Visibility.Hidden;
            ErrorTextContent.Text = "";
            TryAgainButton.Visibility = Visibility.Hidden;
            OnAuthorizedUser?.Invoke(this, e);

            _authorized = false;
            _authorizationInProgress = false;
            LogInView.Visibility = Visibility.Visible;
            LoadingView.Visibility = Visibility.Hidden;

            // Bring fron the window to get the users attention
            Activate();

            _authInitByUser = false;

            // Window is hidden so it can be opened again by the application
            Hide();
        }

        private void OnClientAuthorizationFailed(object? sender, OnAuthorizationFailedEventArgs e)
        {
            if (!_authInitByUser)
            {
                _authorized = false;
                _authorizationInProgress = false;
                LogInView.Visibility = Visibility.Visible;
                LoadingView.Visibility = Visibility.Hidden;
                Activate();
                return;
            }

            _authorized = false;
            _authorizationInProgress = false;
            _authFailMessage = e.Message;
            LoadingRing.Visibility = Visibility.Hidden;
            LoadingText.Text = "";
            ErrorText.Visibility = Visibility.Visible;
            ErrorTextContent.Text = _authFailMessage ?? "Authorization failed.";
            TryAgainButton.Visibility = Visibility.Visible;

            // Bring fron the window to get the users attention
            Activate();

            _authInitByUser = false;
        }

        public void Authorize()
        {
            if (_authorizationInProgress || _authorized)
            {
                return;
            }

            LogInView.Visibility = Visibility.Hidden;
            LoadingView.Visibility = Visibility.Visible;

            _authorized = false;
            _authorizationInProgress = true;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingText.Text = _authInitByUser ? "Please wait while a browser window opens..." : "Please wait...";
            ErrorText.Visibility = Visibility.Hidden;
            ErrorTextContent.Text = "";
            TryAgainButton.Visibility = Visibility.Hidden;

            _ = _apiService.BeginOAuthAsync(OnClientAuthorized, OnClientAuthorizationFailed, !_authInitByUser);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            _authInitByUser = false;
            Authorize();
        }

        private void AuthorizeButton_Click(object sender, RoutedEventArgs e)
        {
            _authInitByUser = true;
            Authorize();
        }
    }
}

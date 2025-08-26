using System.Windows.Input;
using CapYap.API.Models.Events;
using CapYap.Interfaces;
using CapYap.Models;
using CapYap.ViewModels.Windows;

namespace CapYap.Views.Windows
{
    public partial class LoginWindow
    {
        public LoginWindowViewModel ViewModel { get; }

        private readonly IAuthorizationService _authService;

        private bool _authorized = false;
        private bool _authorizationInProgress = false;
        private bool _error = false;
        private string? _authFailMessage = null;
        private bool _authInitByUser = false;

        public event EventHandler<AuthorizedUserEventArgs?> OnAuthorizedUser;

        public LoginWindow(
            LoginWindowViewModel viewModel,
            IAuthorizationService authorizationService
        )
        {
            ViewModel = viewModel;

            _authService = authorizationService;

            InitializeComponent();
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
            _error = false;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingText.Text = "Loading...";
            ErrorText.Visibility = Visibility.Hidden;
            ErrorTextContent.Text = "";
            TryAgainButton.Visibility = Visibility.Hidden;
            OnAuthorizedUser?.Invoke(this, e);

            _authorized = false;
            _authorizationInProgress = false;
            _error = false;
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
                _error = false;
                LogInView.Visibility = Visibility.Visible;
                LoadingView.Visibility = Visibility.Hidden;
                Activate();
                return;
            }

            _authorized = false;
            _authorizationInProgress = false;
            _error = true;
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
            _error = false;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingText.Text = _authInitByUser ? "Please wait while a browser window opens..." : "Please wait...";
            ErrorText.Visibility = Visibility.Hidden;
            ErrorTextContent.Text = "";
            TryAgainButton.Visibility = Visibility.Hidden;

            _ = _authService.BeginOAuthAsync(OnClientAuthorized, OnClientAuthorizationFailed, !_authInitByUser);
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

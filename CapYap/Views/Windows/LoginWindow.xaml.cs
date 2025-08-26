using CapYap.API;
using CapYap.Interfaces;
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
            if (!_authorized)
            {
                Application.Current.Shutdown();
            }
        }

        private void OnClientAuthorized(object? sender, EventArgs e)
        {
            _authorized = true;
            _authorizationInProgress = false;
            _error = false;
            ErrorText.Text = "";
            Close();
        }

        private void OnClientAuthorizationFailed(object? sender, OnAuthorizationFailedEventArgs e)
        {
            _authorized = false;
            _authorizationInProgress = false;
            _error = true;
            _authFailMessage = e.Message;
            ErrorText.Text = _authFailMessage ?? "Authorization failed.";
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            if (!_error)
            {
                return;
            }
            ErrorText.Text = _authFailMessage ?? "Authorization failed.";
        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (_authorizationInProgress || _authorized)
            {
                return;
            }

            _authorized = false;
            _authorizationInProgress = true;
            _error = false;
            ErrorText.Text = "Please wait for a browser window to open...";

            base.OnContentRendered(e);

            _ = _authService.BeginOAuthAsync(OnClientAuthorized, OnClientAuthorizationFailed);
        }
    }
}

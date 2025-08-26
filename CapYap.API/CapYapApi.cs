using System.Net;
using System.Text;
using Appwrite.Models;
using CapYap.API.Utils;

namespace CapYap.API
{
    public class CapYapApi
    {
        private readonly Appwrite _appwrite;
        private readonly HttpClient _httpClient;
        private readonly HttpListener _listener;
        private const int LISTENER_PORT = 19375;

        private readonly string _apiHost = "https://sc.marakusa.me";

        #region Event callbacks
        private delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e);
        private event AsyncEventHandler<OnAuthorizedEventArgs>? OnClientAuthorizedAsync;

        public event EventHandler? OnClientAuthorizationFinished;
        public event EventHandler<OnAuthorizationFailedEventArgs>? OnClientAuthorizationFailed;

        private async Task ApiOnClientAuthorized(object? sender, OnAuthorizedEventArgs e)
        {
            try
            {
                Session session = await _appwrite.account.CreateSession(e.UserId, e.Secret);
                
                if (session == null)
                {
                    _runListenerServer = false;
                    OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("Failed to create a session: session was null"));
                    return;
                }

                OnClientAuthorizationFinished?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _runListenerServer = false;
                OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs($"Failed to create a session: {ex}"));
            }
        }
        #endregion

        public CapYapApi(HttpClient client)
        {
            _appwrite = new Appwrite();
            _httpClient = client;
            _apiHost = "https://sc.marakusa.me";
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{LISTENER_PORT}/");
            OnClientAuthorizedAsync += ApiOnClientAuthorized;
        }

        #region HTTP server to listen for callbacks
        private bool _runListenerServer = true;

        private async Task StartHttpListenerAsync(HttpListener listener)
        {
            listener.Start();

            try
            {
                await HandleIncomingConnectionsAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP listener failed: {ex}");
            }
            finally
            {
                listener.Close();
            }
        }

        private async Task HandleIncomingConnectionsAsync()
        {
            while (_runListenerServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await _listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                byte[] data;

                switch (req.Url?.AbsolutePath)
                {
                    case "/success":
                        string? userId = req.QueryString["userId"];
                        string? secret = req.QueryString["secret"];

                        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(secret))
                        {
                            // Write the response info
                            data = Encoding.UTF8.GetBytes("Failed to authorize client. Please try again or contact the author of the app.");
                            resp.ContentType = "text/html";
                            resp.ContentEncoding = Encoding.UTF8;
                            resp.ContentLength64 = data.LongLength;

                            // Write out to the response stream (asynchronously), then close it
                            await resp.OutputStream.WriteAsync(data, 0, data.Length);
                            resp.Close();

                            OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("No user ID or secret received from the provider"));
                            return;
                        }

                        if (OnClientAuthorizedAsync != null)
                        {
                            // Write the response info
                            data = Encoding.UTF8.GetBytes("Client successfully authorized. You can close this window now.");
                            resp.ContentType = "text/html";
                            resp.ContentEncoding = Encoding.UTF8;
                            resp.ContentLength64 = data.LongLength;

                            // Write out to the response stream (asynchronously), then close it
                            await resp.OutputStream.WriteAsync(data, 0, data.Length);
                            resp.Close();

                            await OnClientAuthorizedAsync.Invoke(this, new OnAuthorizedEventArgs(userId, secret));
                        }

                        // Shutdown server, it is not needed anymore
                        _runListenerServer = false;
                        break;

                    case "/failure":
                        // Write the response info
                        data = Encoding.UTF8.GetBytes("Failed to authorize client. Please try again or contact the author of the app.");
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();

                        OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("Failed callback received from the server"));

                        // Shutdown server, it is not needed anymore
                        _runListenerServer = false;
                        break;

                    // Some other path not recognized
                    default:
                        // Write the response info
                        data = Encoding.UTF8.GetBytes($"Failed to {req.HttpMethod} {req.Url?.AbsolutePath}");
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;
                        resp.StatusCode = 404;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();

                        break;
                }
            }
        }
        #endregion

        public async Task CreateSessionAsync(string userId, string secret)
        {
            try
            {
                Session session = await _appwrite.account.CreateSession(userId, secret);

                if (session == null)
                {
                    _runListenerServer = false;
                    OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("Failed to create a session: session was null"));
                    return;
                }

                OnClientAuthorizationFinished?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _runListenerServer = false;
                OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs($"Failed to create a session: {ex}"));
            }
        }

        public async Task OAuthAuthorizeAsync()
        {
            AppUtils.OpenUrl($"{_apiHost}/oauth?desktop");

            if (!_listener.IsListening)
            {
                await StartHttpListenerAsync(_listener);
            }
        }

        public async Task<bool> IsClientAuthorizedAsync()
        {
            try
            {
                User user = await _appwrite.account.Get();
                return user != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account.Get returned an error: {ex}");
                return false;
            }
        }
    }
}

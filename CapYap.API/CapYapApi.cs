using System.Net;
using System.Text;
using CapYap.API.Models;
using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Events;
using CapYap.API.Utils;
using Newtonsoft.Json;

namespace CapYap.API
{
    public class CapYapApi
    {
        private readonly Appwrite _appwrite;
        private readonly HttpClient _httpClient;
        private readonly string _apiHost = "https://sc.marakusa.me";

        private Session? _currentSession;

        #region Event callbacks
        private delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e);
        private event AsyncEventHandler<OnAuthorizedEventArgs>? OnClientAuthorizedAsync;

        public event EventHandler<AuthorizedUserEventArgs>? OnClientAuthorizationFinished;
        public event EventHandler<OnAuthorizationFailedEventArgs>? OnClientAuthorizationFailed;

        private async Task ApiOnClientAuthorized(object? sender, OnAuthorizedEventArgs e)
        {
            await CreateSessionAsync(e.UserId, e.Secret);
        }
        #endregion

        #region Handle cookies
        private readonly CookieContainer _cookieContainer;

        private void SaveCookies()
        {
            string userSessionConfigFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap");
            string userSessionConfigPath = Path.Combine(userSessionConfigFolderPath, "usrSes.dat");

            if (!Directory.Exists(userSessionConfigFolderPath))
            {
                Directory.CreateDirectory(userSessionConfigFolderPath);
            }

            string cookieData = JsonConvert.SerializeObject(_cookieContainer.GetAllCookies());
            File.WriteAllText(userSessionConfigPath, cookieData);
        }

        private CookieCollection? LoadCookies()
        {
            string userSessionConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "usrSes.dat");
            if (File.Exists(userSessionConfigPath))
            {
                string cookieData = File.ReadAllText(userSessionConfigPath);
                return JsonConvert.DeserializeObject<CookieCollection?>(cookieData);
            }

            return null;
        }
        #endregion

        public CapYapApi(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _cookieContainer = new CookieContainer();

            CookieCollection? loadedCookies = LoadCookies();
            if (loadedCookies != null)
            {
                _cookieContainer.Add(loadedCookies);
            }

            HttpClientHandler clientHandler = new()
            {
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = _cookieContainer
            };
            _appwrite = new Appwrite(new HttpClient(clientHandler), SaveCookies);
            _apiHost = "https://sc.marakusa.me";

            OnClientAuthorizedAsync += ApiOnClientAuthorized;
        }

        #region HTTP server to listen for callbacks
        private bool _listenerRunning = false;
        private const int LISTENER_PORT = 19375;
        private bool _runListenerServer = true;

        private async Task StartHttpListenerAsync()
        {
            using HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{LISTENER_PORT}/");
            listener.Start();
            _runListenerServer = true;

            try
            {
                await HandleIncomingConnectionsAsync(listener);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"HTTP listener failed: {ex}");
            }
            finally
            {
                listener.Close();
                _listenerRunning = false;
            }
        }

        private async Task HandleIncomingConnectionsAsync(HttpListener listener)
        {
            _listenerRunning = true;

            while (_runListenerServer)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

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

                        // Write the response info
                        data = Encoding.UTF8.GetBytes("Client successfully authorized. You may now close this window.");
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();

                        if (OnClientAuthorizedAsync != null)
                        {
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

        #region API functions
        public async Task OAuthAuthorizeAsync()
        {
            AppUtils.OpenUrl($"{_apiHost}/oauth?desktop");

            if (!_listenerRunning)
            {
                await StartHttpListenerAsync();
            }
        }

        public async Task<Gallery?> FetchGalleryAsync(int page)
        {
            try
            {
                string jwt = await _appwrite.CheckJWT();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{_apiHost}/f/fetchGallery?limit=24&page={page}")
                {
                    Content = new StringContent(
                        JsonConvert.SerializeObject(new CapYapApiJwtRequest(jwt)),
                        Encoding.UTF8,
                        "application/json"
                    )
                };
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseData = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Gallery? gallery = JsonConvert.DeserializeObject<Gallery?>(responseData);
                    if (gallery != null)
                    {
                        return gallery;
                    }
                    throw new Exception("Gallery was null.");
                }
                else
                {
                    throw new Exception(responseData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchGallery returned an error: {ex}");
                return null;
            }
        }

        public async Task UploadCaptureAsync(string filePath)
        {
            try
            {
                string jwt = await _appwrite.CheckJWT();

                MultipartFormDataContent form = new()
                {
                    { new StringContent(jwt), "sessionKey" }
                };

                var capContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
                form.Add(capContent, "file", Path.GetFileName(filePath));

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{_apiHost}/f/upload")
                {
                    Content = form
                };
                HttpResponseMessage response = await _httpClient.SendAsync(request);
                string responseData = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(responseData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FetchGallery returned an error: {ex}");
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region Appwrite functions
        public async Task CreateSessionAsync(string userId, string secret)
        {
            try
            {
                _currentSession = await _appwrite.CreateSessionAsync(userId, secret);

                if (_currentSession == null)
                {
                    _runListenerServer = false;
                    OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("Failed to create a session: session was null"));
                    return;
                }

                User? user = await GetUserAsync();

                if (user == null)
                {
                    _runListenerServer = false;
                    OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs("Failed to fetch user: user was null"));
                    return;
                }

                OnClientAuthorizationFinished?.Invoke(this, new AuthorizedUserEventArgs(user));
            }
            catch (Exception ex)
            {
                _runListenerServer = false;
                OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs($"Failed to create a session: {ex}"));
            }
        }

        public async Task<bool> IsClientAuthorizedAsync()
        {
            try
            {
                User? user = await _appwrite.GetAccountAsync();
                return user != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account.Get returned an error: {ex}");
                return false;
            }
        }

        public async Task<User?> GetUserAsync()
        {
            try
            {
                User? user = await _appwrite.GetAccountAsync();
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account.Get returned an error: {ex}");
                OnClientAuthorizationFailed?.Invoke(this, new OnAuthorizationFailedEventArgs(ex.ToString()));
                return null;
            }
        }

        public async Task DeleteSessionAsync()
        {
            try
            {
                await _appwrite.DeleteSessionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DELETE Account.Session returned an error: {ex}");
            }
        }
        #endregion
    }
}

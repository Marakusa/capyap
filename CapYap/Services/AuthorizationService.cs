using System.Net.Http;
using CapYap.API;
using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Events;
using CapYap.Interfaces;

namespace CapYap.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly CapYapApi _api;

        public event EventHandler<User?>? OnUserChanged;
        public event EventHandler<List<string>?>? OnGalleryChanged;

        public AuthorizationService(HttpClient client)
        {
            _api = new CapYapApi(client);
        }

        public async Task BeginOAuthAsync(Action<object?, AuthorizedUserEventArgs> successCallback, Action<object?, OnAuthorizationFailedEventArgs> failedCallback, bool checkOnly = false)
        {
            if (successCallback != null)
            {
                _api.OnClientAuthorizationFinished += (object? sender, AuthorizedUserEventArgs e) =>
                {
                    OnUserChanged?.Invoke(this, e.Data);
                    successCallback.Invoke(null, e);
                };
            }

            if (failedCallback != null)
            {
                _api.OnClientAuthorizationFailed += (object? sender, OnAuthorizationFailedEventArgs e) =>
                {
                    failedCallback.Invoke(null, e);
                };
            }

            bool isClientAuthorized = await _api.IsClientAuthorizedAsync();

            if (isClientAuthorized)
            {
                if (successCallback != null)
                {
                    User? user = await _api.GetUserAsync();

                    OnUserChanged?.Invoke(this, user);

                    if (user != null)
                    {
                        successCallback.Invoke(null, new AuthorizedUserEventArgs(user));
                        return;
                    }
                }
            }

            if (!checkOnly)
            {
                await _api.OAuthAuthorizeAsync();
            }
            else
            {
                failedCallback?.Invoke(null, new OnAuthorizationFailedEventArgs("User session changed. Please relogin."));
            }
        }

        public async Task<bool> IsAuthorizedAsync()
        {
            try
            {
                return await _api.IsClientAuthorizedAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to authorize user: {ex}");
                return false;
            }
        }

        public async Task<User?> GetUserAsync()
        {
            try
            {
                User? user = await _api.GetUserAsync();
                OnUserChanged?.Invoke(this, user);
                return user;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch user: {ex}");
                return null;
            }
        }

        public async Task LogOutAsync()
        {
            try
            {
                await _api.DeleteSessionAsync();
                OnUserChanged?.Invoke(this, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to log user out: {ex}");
            }
        }

        public async Task<List<string>?> FetchGalleryAsync()
        {
            try
            {
                List<string>? gallery = await _api.FetchGalleryAsync();
                OnGalleryChanged?.Invoke(this, gallery);
                return gallery;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to fetch user: {ex}");
                return null;
            }
        }
    }
}

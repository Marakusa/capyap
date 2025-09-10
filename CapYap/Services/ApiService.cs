using CapYap.API;
using CapYap.API.Models;
using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Events;
using CapYap.Interfaces;
using CapYap.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace CapYap.Services
{
    public class ApiService : IApiService
    {
        private readonly ILogger<ApiService> _log;
        private readonly CapYapApi _api;

        public event EventHandler<GalleryChangedEventArgs>? OnGalleryChanged;
        public event Action<EventArgs>? OnGalleryFetching;
        public event EventHandler<StatsChangedEventArgs>? OnStatsChanged;
        public event Action<EventArgs>? OnStatsFetching;
        public event EventHandler<User?>? OnUserChanged;

        private int _currentPage = 1;

        public ApiService(ILogger<ApiService> log, HttpClient client)
        {
            _log = log;
            _api = new CapYapApi(client);
            _currentPage = 1;
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
                _log.LogError($"Failed to authorize user: {ex}");
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
                _log.LogError($"Failed to fetch user: {ex}");
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
                _log.LogError($"Failed to log user out: {ex}");
            }
        }

        public async Task<Gallery?> FetchGalleryAsync()
        {
            return await FetchGalleryAsync(_currentPage);
        }

        public async Task<Gallery?> FetchGalleryNextAsync()
        {
            _currentPage++;
            return await FetchGalleryAsync(_currentPage);
        }

        public async Task<Gallery?> FetchGalleryPrevAsync()
        {
            _currentPage--;
            if (_currentPage < 1)
            {
                _currentPage = 1;
            }
            return await FetchGalleryAsync(_currentPage);
        }

        public async Task<Gallery?> FetchGalleryAsync(int page)
        {
            try
            {
                OnGalleryFetching?.Invoke(EventArgs.Empty);
                _currentPage = page;
                Gallery? gallery = await _api.FetchGalleryAsync(_currentPage);
                OnGalleryChanged?.Invoke(this, new GalleryChangedEventArgs(gallery));
                return gallery;
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to fetch gallery: {ex}");
                OnGalleryChanged?.Invoke(this, new GalleryChangedEventArgs(null, true, ex.Message));
                return null;
            }
        }

        public async Task<Stats?> FetchStatsAsync()
        {
            try
            {
                OnStatsFetching?.Invoke(EventArgs.Empty);
                Stats? stats = await _api.FetchStatsAsync();
                OnStatsChanged?.Invoke(this, new StatsChangedEventArgs(stats));
                return stats;
            }
            catch (Exception ex)
            {
                _log.LogError($"Failed to fetch gallery: {ex}");
                OnStatsChanged?.Invoke(this, new StatsChangedEventArgs(null, true, ex.Message));
                return null;
            }
        }

        public async Task<string> UploadCaptureAsync(string path, int quality = -1, int level = -1)
        {
            return await _api.UploadCaptureAsync(path, quality, level);
        }

        public async Task DeleteCaptureAsync(string? url)
        {
            await _api.DeleteCaptureAsync(url);
        }
    }
}

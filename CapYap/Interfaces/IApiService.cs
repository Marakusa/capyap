using CapYap.API.Models;
using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Events;
using CapYap.Models;

namespace CapYap.Interfaces
{
    public interface IApiService
    {
        public event EventHandler<GalleryChangedEventArgs>? OnGalleryChanged;
        public event Action<EventArgs>? OnGalleryFetching;
        public event EventHandler<StatsChangedEventArgs>? OnStatsChanged;
        public event Action<EventArgs>? OnStatsFetching;
        public event EventHandler<User?>? OnUserChanged;

        public Task BeginOAuthAsync(Action<object?, AuthorizedUserEventArgs> successCallback, Action<object?, OnAuthorizationFailedEventArgs> failedCallback, bool checkOnly = false);

        public Task<bool> IsAuthorizedAsync();

        public Task<User?> GetUserAsync();

        public Task LogOutAsync();

        public Task<Gallery?> FetchGalleryAsync();

        public Task<Gallery?> FetchGalleryNextAsync();

        public Task<Gallery?> FetchGalleryPrevAsync();

        public Task<Gallery?> FetchGalleryAsync(int page);

        public Task<Stats?> FetchStatsAsync();

        public Task<string> UploadCaptureAsync(string path);
    }
}

using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Events;

namespace CapYap.Interfaces
{
    public interface IAuthorizationService
    {
        public event EventHandler<List<string>?>? OnGalleryChanged;
        public event EventHandler<User?>? OnUserChanged;

        public Task BeginOAuthAsync(Action<object?, AuthorizedUserEventArgs> successCallback, Action<object?, OnAuthorizationFailedEventArgs> failedCallback, bool checkOnly = false);

        public Task<bool> IsAuthorizedAsync();

        public Task<User?> GetUserAsync();

        public Task LogOutAsync();

        public Task<List<string>?> FetchGalleryAsync();
    }
}

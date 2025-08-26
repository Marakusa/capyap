using CapYap.API;

namespace CapYap.Interfaces
{
    public interface IAuthorizationService
    {
        public Task BeginOAuthAsync(Action<object?, EventArgs> successCallback, Action<object?, OnAuthorizationFailedEventArgs> failedCallback);

        public Task<bool> IsAuthorizedAsync();
    }
}

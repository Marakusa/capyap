using System.IO;
using System.Net.Http;
using Appwrite.Models;
using CapYap.API;
using CapYap.Interfaces;
using Config.Net;

namespace CapYap.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserSettings _userSettings;
        private readonly CapYapApi _api;

        public AuthorizationService(HttpClient client)
        {
            string userSessionConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapYap", "userSession.json");
            IUserSettings settings = new ConfigurationBuilder<IUserSettings>()
               .UseJsonFile(userSessionConfigPath)
               .Build();

            _userSettings = settings;
            _api = new CapYapApi(client);
        }

        public async Task BeginOAuthAsync(Action<object?, EventArgs> successCallback, Action<object?, OnAuthorizationFailedEventArgs> failedCallback)
        {
            if (successCallback != null)
            {
                _api.OnClientAuthorizationFinished += (object? sender, EventArgs e) =>
                {
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
                    successCallback.Invoke(null, EventArgs.Empty);
                }
                return;
            }

            await _api.OAuthAuthorizeAsync();
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
    }
}

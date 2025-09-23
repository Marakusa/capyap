using CapYap.API.Models.Appwrite;
using CapYap.API.Models.Exceptions;
using Newtonsoft.Json;

namespace CapYap.API
{
    internal class Appwrite
    {
        private readonly HttpClient _client;

        private readonly string _endpoint = "https://aw.marakusa.me/v1";
        private readonly string _projectId = "67e6390300179aa2b086";

        private DateTime? _jwtKeyExpire = null;
        private string? _jwtKey = null;

        private readonly Action _saveCookiesCall;

        public Appwrite(HttpClient client, Action saveCookies)
        {
            _client = client;
            _saveCookiesCall = saveCookies;
        }

        public Uri GetHost()
        {
            return new Uri(_endpoint);
        }

        public async Task<Session?> CreateSessionAsync(string userId, string secret)
        {
            SessionTokenRequest sessionTokenRequest = new()
            {
                UserId = userId,
                Secret = secret
            };
            return await SendAsync<Session>(
                HttpMethod.Post,
                "/account/sessions/token",
                content: new StringContent(
                    JsonConvert.SerializeObject(sessionTokenRequest),
                    System.Text.Encoding.UTF8,
                    "application/json"));
        }

        private async Task<JWT> CreateJWTAsync()
        {
            DateTime expireTime = DateTime.UtcNow.AddMinutes(15);

            JWT? jwt = await SendAsync<JWT>(HttpMethod.Post, "/account/jwts") ?? throw new AppwriteException("No JWT key was returned from Appwrite.");

            _jwtKey = jwt.Jwt;
            _jwtKeyExpire = expireTime;
            return jwt;
        }

        public async Task<User?> GetAccountAsync()
        {
            _jwtKey = await CheckJWT();

            return await SendAsync<User>(
                HttpMethod.Get,
                "/account",
                new Dictionary<string, string>
                {
                    {"X-Appwrite-JWT", _jwtKey}
                });
        }

        public async Task DeleteSessionAsync()
        {
            _jwtKey = await CheckJWT();
            await SendAsync<User>(
                HttpMethod.Delete,
                "/account/sessions/current",
                new Dictionary<string, string>
                {
                    {"X-Appwrite-JWT", _jwtKey}
                });
            _jwtKey = null;
            _jwtKeyExpire = null;
        }

        public async Task<string> CheckJWT()
        {
            if (_jwtKey != null && DateTime.UtcNow < _jwtKeyExpire)
            {
                return _jwtKey;
            }

            await CreateJWTAsync();

            if (_jwtKey == null)
            {
                throw new AppwriteException("Generate a JWT key before using this endpoint.");
            }

            return _jwtKey;
        }

        public async Task<T?> SendAsync<T>(HttpMethod method, string endpointPath, Dictionary<string, string>? headers = null, HttpContent? content = null)
        {
            HttpRequestMessage request = new(method, $"{_endpoint}{endpointPath}")
            {
                Content = content
            };

            // Add headers
            request.Headers.Add("X-Appwrite-Response-Format", "1.6.0");
            request.Headers.Add("X-Appwrite-Project", _projectId);
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            HttpResponseMessage response = await _client.SendAsync(request);

            // Save cookies to save the session
            _saveCookiesCall.Invoke();

            string responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonConvert.DeserializeObject<T>(responseContent);
            }

            AppwriteErrorResponse? errorResponse = JsonConvert.DeserializeObject<AppwriteErrorResponse>(responseContent);

            if (errorResponse != null)
            {
                throw new AppwriteException(errorResponse.Message, errorResponse.Code, errorResponse.Type, errorResponse.Version);
            }
            throw new AppwriteException($"Appwrite responded with an error from {method.Method} {endpointPath}");
        }
    }
}

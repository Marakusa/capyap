using Newtonsoft.Json;

namespace CapYap.API.Models.Appwrite
{
    internal class SessionTokenRequest
    {
        [JsonProperty("userId")]
        public string? UserId { get; set; }

        [JsonProperty("secret")]
        public string? Secret { get; set; }
    }
}

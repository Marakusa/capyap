using Newtonsoft.Json;

namespace CapYap.API.Models
{
    internal class CapYapApiJwtRequest
    {
        [JsonProperty("sessionKey")]
        public string SessionKey { get; set; }

        public CapYapApiJwtRequest(string sessionKey)
        {
            SessionKey = sessionKey;
        }
    }
}

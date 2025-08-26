using Newtonsoft.Json;

namespace CapYap.API.Models.Appwrite
{
    public class JWT
    {
        [JsonProperty("jwt")]
        public string Jwt { get; set; }
    }
}

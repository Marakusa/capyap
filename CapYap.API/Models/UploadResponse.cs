using Newtonsoft.Json;

namespace CapYap.API.Models
{
    public class UploadResponse
    {
        [JsonProperty("url")]
        public string? Url { get; set; }
    }
}

using Newtonsoft.Json;

namespace CapYap.API.Models.Appwrite
{
    internal class AppwriteErrorResponse
    {
        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("code")]
        public int? Code { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("version")]
        public string? Version { get; set; }
    }
}

using Newtonsoft.Json;

namespace CapYap.API.Models
{
    public class Gallery
    {
        [JsonProperty("total")]
        public int Total { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("page")]
        public int Page { get; set; }

        [JsonProperty("totalPages")]
        public int TotalPages { get; set; }

        [JsonProperty("documents")]
        public List<string>? Documents { get; set; }
    }
}

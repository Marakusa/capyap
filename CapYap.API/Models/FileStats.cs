using Newtonsoft.Json;

namespace CapYap.API.Models
{
    public class FileStats
    {
        [JsonProperty("size")]
        public string Size { get; set; } = "0 KB";

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("uploadedAt")]
        public DateTime UploadedAt { get; set; }
    }
}

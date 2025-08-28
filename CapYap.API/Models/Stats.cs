using Newtonsoft.Json;

namespace CapYap.API.Models
{
    public class Stats
    {
        [JsonProperty("spaceUsed")]
        public string SpaceUsed { get; set; } = "0 KB";

        [JsonProperty("views")]
        public int Views { get; set; }

        [JsonProperty("files7Days")]
        public int Files7Days { get; set; }

        [JsonProperty("totalFiles")]
        public int TotalFiles { get; set; }
    }
}

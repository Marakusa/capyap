using Newtonsoft.Json;

namespace CapYap.API.Models.Appwrite
{
    public class UserHashOptions
    {
    }

    public class User
    {
        [JsonProperty("$id")]
        public string? Id { get; set; }

        [JsonProperty("$createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("$updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("password")]
        public string? Password { get; set; }

        [JsonProperty("hash")]
        public string? Hash { get; set; }

        [JsonProperty("hashOptions")]
        public UserHashOptions? HashOptions { get; set; }

        [JsonProperty("registration")]
        public DateTime? Registration { get; set; }

        [JsonProperty("status")]
        public bool? Status { get; set; }

        [JsonProperty("labels")]
        public List<string>? Labels { get; set; }

        [JsonProperty("passwordUpdate")]
        public DateTime? PasswordUpdate { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }

        [JsonProperty("phone")]
        public string? Phone { get; set; }

        [JsonProperty("emailVerification")]
        public bool? EmailVerification { get; set; }

        [JsonProperty("phoneVerification")]
        public bool? PhoneVerification { get; set; }

        [JsonProperty("mfa")]
        public bool? Mfa { get; set; }

        [JsonProperty("prefs")]
        public Dictionary<string, string>? Prefs { get; set; }

        [JsonProperty("targets")]
        public List<object>? Targets { get; set; }

        [JsonProperty("accessedAt")]
        public DateTime? AccessedAt { get; set; }
    }
}

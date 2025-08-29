using Newtonsoft.Json;

namespace CapYap.API.Models.Appwrite
{
    public class Session
    {
        [JsonProperty("$id")]
        public string? Id { get; set; }

        [JsonProperty("$createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonProperty("$updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonProperty("userId")]
        public string? UserId { get; set; }

        [JsonProperty("expire")]
        public DateTime? Expire { get; set; }

        [JsonProperty("provider")]
        public string? Provider { get; set; }

        [JsonProperty("providerUid")]
        public string? ProviderUid { get; set; }

        [JsonProperty("providerAccessToken")]
        public string? ProviderAccessToken { get; set; }

        [JsonProperty("providerAccessTokenExpiry")]
        public DateTime? ProviderAccessTokenExpiry { get; set; }

        [JsonProperty("providerRefreshToken")]
        public string? ProviderRefreshToken { get; set; }

        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("osCode")]
        public string? OsCode { get; set; }

        [JsonProperty("osName")]
        public string? OsName { get; set; }

        [JsonProperty("osVersion")]
        public string? OsVersion { get; set; }

        [JsonProperty("clientType")]
        public string? ClientType { get; set; }

        [JsonProperty("clientCode")]
        public string? ClientCode { get; set; }

        [JsonProperty("clientName")]
        public string? ClientName { get; set; }

        [JsonProperty("clientVersion")]
        public string? ClientVersion { get; set; }

        [JsonProperty("clientEngine")]
        public string? ClientEngine { get; set; }

        [JsonProperty("clientEngineVersion")]
        public string? ClientEngineVersion { get; set; }

        [JsonProperty("deviceName")]
        public string? DeviceName { get; set; }

        [JsonProperty("deviceBrand")]
        public string? DeviceBrand { get; set; }

        [JsonProperty("deviceModel")]
        public string? DeviceModel { get; set; }

        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("countryName")]
        public string? CountryName { get; set; }

        [JsonProperty("current")]
        public bool Current { get; set; }

        [JsonProperty("factors")]
        public List<string>? Factors { get; set; }

        [JsonProperty("secret")]
        public string? Secret { get; set; }

        [JsonProperty("mfaUpdatedAt")]
        public DateTime? MfaUpdatedAt { get; set; }
    }
}

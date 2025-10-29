using System.Text.Json.Serialization;

namespace _3TLMiniSoccer.Models
{
    public class BankInfo
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; } = string.Empty;

        [JsonPropertyName("bin")]
        public string Bin { get; set; } = string.Empty;

        [JsonPropertyName("supported")]
        public bool Supported { get; set; }
    }

    public class BanksResponse
    {
        [JsonPropertyName("no_banks")]
        public string NoBanks { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public List<BankInfo> Data { get; set; } = new List<BankInfo>();
    }
}

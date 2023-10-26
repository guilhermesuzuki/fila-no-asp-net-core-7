using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace fila_no_asp_net_core_7.Models
{
    [ExcludeFromCodeCoverage]
    public class Image
    {
        [JsonPropertyName("FileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("base64")]
        public string Base64 { get; set; } = string.Empty;
    }
}

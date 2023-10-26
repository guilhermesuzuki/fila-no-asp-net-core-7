using fila_no_asp_net_core_7.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace fila_no_asp_net_core_7.Models
{
    [ExcludeFromCodeCoverage]
    public class SaveResponse : IRequestId
    {
        [JsonPropertyName("RequestId")]
        public int RequestId { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; } = string.Empty;
    }
}

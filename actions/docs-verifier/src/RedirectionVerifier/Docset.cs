using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public sealed class Docset
    {
        [JsonPropertyName("redirection_files")]
        public List<string>? RedirectionFiles { get; set; } = null;
    }
}

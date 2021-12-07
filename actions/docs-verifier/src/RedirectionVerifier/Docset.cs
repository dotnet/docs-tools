using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public sealed class Docset
    {
        [JsonPropertyName("redirection_files")]
        public ImmutableArray<string>? RedirectionFiles { get; set; } = null;
    }
}

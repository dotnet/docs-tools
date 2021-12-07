using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public sealed class OpenPublishingConfig
    {
        [JsonPropertyName("docsets_to_publish")]
        public ImmutableArray<Docset> Docsets { get; set; }
    }
}

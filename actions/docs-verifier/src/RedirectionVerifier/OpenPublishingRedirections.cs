using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public sealed class OpenPublishingRedirections
    {
        [JsonPropertyName("redirections")]
        public ImmutableArray<Redirection> Redirections { get; set; }
    }
}

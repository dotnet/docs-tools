using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes → JSON
    internal sealed class OpenPublishingRedirections
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
    {
        [JsonPropertyName("redirections")]
        public ImmutableArray<Redirection> Redirections { get; set; }
    }
}

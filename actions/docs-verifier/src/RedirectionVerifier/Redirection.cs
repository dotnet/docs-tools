using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes → JSON
    internal sealed class Redirection
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
    {
        [JsonPropertyName("source_path")]
        public string SourcePath { get; set; } = null!;

        [JsonPropertyName("redirect_url")]
        public string RedirectUrl { get; set; } = null!;
    }
}

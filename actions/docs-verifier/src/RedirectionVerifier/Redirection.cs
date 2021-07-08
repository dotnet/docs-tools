using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
#pragma warning disable CA1812 // Avoid uninstantiated internal classes → JSON
    public sealed class Redirection
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
    {
        [JsonPropertyName("source_path")]
        public string SourcePath { get; set; } = null!;

        [JsonPropertyName("redirect_url")]
#pragma warning disable CA1056 // URI-like properties should not be strings → Could throw
        public string RedirectUrl { get; set; } = null!;
#pragma warning restore CA1056 // URI-like properties should not be strings
    }
}

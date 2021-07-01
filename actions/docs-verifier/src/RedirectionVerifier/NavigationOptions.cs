using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    public record NavigationOptions(
        [property: JsonPropertyName("repoTocFolder")] string? RepoTocFolder,
        [property: JsonPropertyName("repoIndexFolder")] string? RepoIndexFolder)
    {
        /// <summary>
        /// The configured path for the "What's new" content.
        /// </summary>
        internal string? WhatsNewPath => RepoTocFolder ?? RepoIndexFolder;
    }
}

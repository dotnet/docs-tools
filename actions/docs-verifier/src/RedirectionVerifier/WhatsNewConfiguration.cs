using System.Text.Json.Serialization;

namespace RedirectionVerifier
{
    /// <summary>
    /// Schema: https://whatsnewapi.azurewebsites.net/schema
    /// Omitted parts of this that are not relevant to our needs.
    /// </summary>
    public record WhatsNewConfiguration(
        [property: JsonPropertyName("docSetProductName")] string DocSetProductName,
        [property: JsonPropertyName("navigationOptions")] NavigationOptions? NavigationOptions);
}

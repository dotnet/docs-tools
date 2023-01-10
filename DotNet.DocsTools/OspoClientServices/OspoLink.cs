// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

using System.Text.Json.Serialization;

namespace Microsoft.DotnetOrg.Ospo;

#pragma warning disable CS8618 // This is a serialized type.
public sealed class OspoLink
{
    [JsonPropertyName("github")]
    public GitHubInfo GitHubInfo { get; set; }

    [JsonPropertyName("aad")]
    public MicrosoftInfo MicrosoftInfo { get; set; }
}
#pragma warning restore CS8618

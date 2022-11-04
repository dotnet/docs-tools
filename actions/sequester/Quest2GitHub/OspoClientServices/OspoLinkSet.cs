// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

namespace Microsoft.DotnetOrg.Ospo;

#pragma warning disable CS8618 // This is a serialized type.
public sealed class OspoLinkSet
{
    public OspoLinkSet()
    {
    }

    public void Initialize()
    {
        LinkByLogin = Links.ToDictionary(l => l.GitHubInfo.Login);
    }

    public IReadOnlyList<OspoLink> Links { get; set; } = new List<OspoLink>();

    [JsonIgnore]
    public IReadOnlyDictionary<string, OspoLink> LinkByLogin { get; set; } = new Dictionary<string, OspoLink>();
}
#pragma warning restore CS8618


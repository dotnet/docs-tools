// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

namespace Microsoft.DotnetOrg.Ospo;

#pragma warning disable CS8618 // This is a serialized type.
public sealed class GitHubInfo
{
    public int Id { get; set; }
    public string Login { get; set; }
    public List<string> Organizations { get; set; }
}
#pragma warning restore CS8618


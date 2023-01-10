// Taken from https://github.com/dotnet/org-policy/tree/main/src/Microsoft.DotnetOrg.Ospo

namespace Microsoft.DotnetOrg.Ospo;

#pragma warning disable CS8618 // This is a serialized type.
public sealed class MicrosoftInfo
{
    public string Alias { get; set; }
    public string PreferredName { get; set; }
    public string UserPrincipalName { get; set; }
    public string EmailAddress { get; set; }
    public string Id { get; set; }
}
#pragma warning restore CS8618


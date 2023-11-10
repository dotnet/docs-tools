using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The Actor node of a PR
/// </summary>
/// <remarks>
/// This node contains the login, and name for this account.
/// </remarks>
public readonly struct Actor(JsonElement authorNode)
{
    /// <summary>
    /// Access the GitHub login for this actor
    /// </summary>
    public string Login { get; } = ResponseExtractors.LoginFromAuthorNode(authorNode);

    /// <summary>
    /// Access the name for this actor.
    /// </summary>
    /// <remarks>
    /// Not all GitHub accounts make the owner's name public.
    /// In that case, the GraphQL node is a null node. However,
    /// since this is primarily used in reporting and human readable 
    /// output, this class translates that condition into the
    /// empty string.
    /// </remarks>
    public string Name { get; } = ResponseExtractors.NameFromAuthorNode(authorNode);

    /// <summary>
    /// Determine if a GitHub login is a Microsoft FTE
    /// </summary>
    /// <param name="ospoClient">The Open Source office client.</param>
    /// <returns>true if a Microsoft FTE. Null if the ospoClient is null, or a bot.</returns>
    public async Task<bool?> IsMicrosoftFTE(OspoClient? ospoClient)
    {
        if ((ospoClient == null) || 
            (Login == "dependabot") ||
            (Login == "github-actions"))
            return null;
        // Non-Microsoft accounts have null for the info
        // Microsoft accounts have non-null. Our bots have no orgs.
        // Note that for "ghost", the gitHubLogin is null or empty here.
        try
        {
            // TODO: This is always called from command line apps. Use the full cache.
            var info = await ospoClient.GetAsync(Login);
            bool? rVal = null;
            if ((info is null) || (info.MicrosoftInfo is null))
                rVal = false;
            else if (info.GitHubInfo.Organizations.Any())
                rVal = true;
            return rVal;
        } catch (OspoException)
        {
            // The service is out. return null:
            return null;
        }
    }
}

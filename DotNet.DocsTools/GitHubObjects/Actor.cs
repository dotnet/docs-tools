using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The Actor node of a PR
/// </summary>
/// <remarks>
/// This node contains the login, and name for this account.
/// If the user has deleted their acount (where the GitHub UI shows "Ghost")
/// then both properties are null.
/// </remarks>
public readonly struct Actor(JsonElement authorNode)
{
    /// <summary>
    /// Access the GitHub login for this actor
    /// </summary>
    /// <remarks>
    /// This is null if the actor has deleted their account.
    /// </remarks>
    public string? Login { get; } = ResponseExtractors.LoginFromAuthorNode(authorNode);

    /// <summary>
    /// Access the name for this actor.
    /// </summary>
    /// <remarks>
    /// Not all GitHub accounts make the owner's name public.
    /// In that case, the GraphQL node is a null node. The login is
    /// non-null in those cases.
    /// </remarks>
    public string? Name { get; } = ResponseExtractors.NameFromAuthorNode(authorNode);

    /// <summary>
    /// Determine if a GitHub login is a Microsoft FTE
    /// </summary>
    /// <param name="ospoClient">The Open Source office client.</param>
    /// <returns>true if a Microsoft FTE. Null if the ospoClient is null, or the account is a bot
    /// or a "ghost" account (where the user has deleted their GitHub account).</returns>
    public async Task<bool?> IsMicrosoftFTE(OspoClient? ospoClient)
    {
        if ((ospoClient == null) || 
            (Login == null) ||
            (Login == "dependabot") ||
            (Login == "github-actions"))
            return null;
        // Non-Microsoft accounts have null for the info
        // Microsoft accounts have non-null. Our bots have no orgs.
        // Note that for "ghost", the gitHubLogin is null or empty here.
        try
        {
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

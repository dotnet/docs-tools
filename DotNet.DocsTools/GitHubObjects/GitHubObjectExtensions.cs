using Microsoft.DotnetOrg.Ospo;

namespace DotNet.DocsTools.GitHubObjects;

public static class GitHubObjectExtensions
{
    /// <summary>
    /// Determine if a GitHub login is a Microsoft FTE
    /// </summary>
    /// <param name="ospoClient">The Open Source office client.</param>
    /// <returns>true if a Microsoft FTE. Null if the ospoClient is null, or the account is a bot
    /// or a "ghost" account (where the user has deleted their GitHub account).</returns>
    public static async Task<bool?> IsMicrosoftFTE(this Actor? actor, OspoClient? ospoClient)
    {
        if ((actor is null) || (ospoClient is null) ||
            (actor.Login == "dependabot") || (actor.Login == "github-actions"))
            return null;
        // Non-Microsoft accounts have null for the info
        // Microsoft accounts have non-null. Our bots have no orgs.
        // Note that for "ghost", the gitHubLogin is null or empty here.
        try
        {
            var info = await ospoClient.GetAsync(actor.Login);
            bool? rVal = null;
            if ((info is null) || (info.MicrosoftInfo is null))
                rVal = false;
            else if (info.GitHubInfo.Organizations.Count > 0)
                rVal = true;
            return rVal;
        }
        catch (OspoException)
        {
            // The service is out. return null:
            return null;
        }
    }
}

using Microsoft.DotnetOrg.Ospo;

namespace DotNet.DocsTools.GitHubObjects;

public static class GitHubObjectExtensions
{
    /// <summary>
    /// Represents a list of well-known excluded actors, most commonly bots.
    /// </summary>
    private static readonly string[] s_wellKnownExcludedActors =
    [
        "dependabot",
        "github-actions",
        "copilot-swe-agent",
        "dotnet-bot",
        "dotnet-maestro",
        "dotnet-policy-service",
        "learn-build-service-prod"
    ];

    /// <summary>
    /// Determine if a GitHub login is a Microsoft FTE
    /// </summary>
    /// <param name="ospoClient">The Open Source office client.</param>
    /// <returns>true if a Microsoft FTE. Null if the ospoClient is null, or the account is a bot
    /// or a "ghost" account (where the user has deleted their GitHub account).</returns>
    public static async Task<bool?> IsMicrosoftFTE(this Actor? actor, OspoClient? ospoClient)
    {
        if ((actor is null) || (ospoClient is null) ||
            (s_wellKnownExcludedActors.Any(
                 excludedActor => string.Equals(actor.Login, excludedActor, StringComparison.OrdinalIgnoreCase))))
        {
            return null;
        }

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

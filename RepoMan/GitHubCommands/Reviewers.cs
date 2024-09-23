using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class Reviewers
{
    /// <summary>
    /// Adds reviewers to the provided <see cref="State.PullRequest"/>.
    /// </summary>
    /// <param name="names">The names of the reviewers. Teams must start with 'team:'.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>An empty task</returns>
    public static async Task AddReviewers(string[] names, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add assignee to an object that doesn't have issue data");
            return;
        }

        if (names.Length != 0)
        {
            List<string>? logins = [];
            List<string>? teams = [];

            foreach (string name in names)
            {
                if (name.StartsWith("team:", StringComparison.OrdinalIgnoreCase))
                    teams.Add(data.ExpandVariables(name.Substring(5)));
                else
                    logins.Add(data.ExpandVariables(name));
            }

            if (logins.Count != 0)
                data.Logger.LogInformation("GitHub: Adding reviewers: {logins}", string.Join(",", logins));
            else
                logins = null;

            if (teams.Count != 0)
                data.Logger.LogInformation("GitHub: Adding reviewer teams: {teams}", string.Join(",", teams));
            else
                teams = null;

            if (!data.IsDryRun)
                await data.GitHubRESTClient.PullRequest.ReviewRequest.Create(data.RepositoryId, data.Issue.Number, new PullRequestReviewRequest(logins, teams));
        }
    }
}

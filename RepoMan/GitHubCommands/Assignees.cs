using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class Assignees
{
    /// <summary>
    /// Adds assignees to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="names">The login names of the users.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>An empty task</returns>
    public static async Task AddAssignees(string[] names, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add assignee to an object that doesn't have issue data");
            return;
        }

        if (names.Length != 0)
        {
            data.Logger.LogInformation("GitHub: Adding assignees: {names}", string.Join(",", names));

            IssueUpdate updateIssue = data.Issue.ToUpdate();

            foreach (string item in names)
                updateIssue.AddAssignee(data.ExpandVariables(item));

            if (!data.IsDryRun)
                await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updateIssue);
        }
    }
}

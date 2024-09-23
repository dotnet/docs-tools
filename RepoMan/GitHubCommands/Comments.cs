using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class Comments
{
    /// <summary>
    /// Creates a comment on an issue or pull request.
    /// </summary>
    /// <param name="comment">The comment body string.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    public static async Task AddComment(string comment, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add comment to an object that doesn't have issue data");
            return;
        }

        data.Logger.LogInformation($"GitHub: Create comment");
        if (!data.IsDryRun)
            await data.GitHubRESTClient.Issue.Comment.Create(data.RepositoryId, data.Issue.Number, data.ExpandVariables(comment));
    }
}

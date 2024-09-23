using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class IssuePullRequest
{
    /// <summary>
    /// Closes an issue or pull request.
    /// </summary>
    /// <param name="data">The state object of the Azure Function.</param>
    public static async Task Close(InstanceData data)
    {
        data.Logger.LogInformation($"GitHub: Close");

        if (data.HasPullRequestData)
        {
            PullRequestUpdate updatedPR = new()
            {
                Base = data.PullRequest.Base.Ref,
                Body = data.PullRequest.Body,
                Title = data.PullRequest.Title,
                State = ItemState.Closed
            };

            if (!data.IsDryRun)
                await data.GitHubRESTClient.PullRequest.Update(data.RepositoryId, data.PullRequest.Number, updatedPR);
        }
        else if (data.HasIssueData)
        {
            IssueUpdate updatedIssue = data.Issue.ToUpdate();
            updatedIssue.State = ItemState.Closed;

            if (!data.IsDryRun)
                await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updatedIssue);

        }
        else
        {
            data.Logger.LogError("Tried to close object, but object is neither issue nor pull request");
        }
    }

    /// <summary>
    /// Opens an issue or pull request.
    /// </summary>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task Open(InstanceData data)
    {
        data.Logger.LogInformation($"GitHub: Open");

        if (data.HasPullRequestData)
        {
            PullRequestUpdate updatedPR = new()
            {
                Base = data.PullRequest.Base.Ref,
                Body = data.PullRequest.Body,
                Title = data.PullRequest.Title,
                State = ItemState.Open
            };

            if (!data.IsDryRun)
                await data.GitHubRESTClient.PullRequest.Update(data.RepositoryId, data.PullRequest.Number, updatedPR);
        }
        else if (data.HasIssueData)
        {
            IssueUpdate updatedIssue = data.Issue.ToUpdate();
            updatedIssue.State = ItemState.Open;

            if (!data.IsDryRun)
                await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updatedIssue);

        }
        else
        {
            data.Logger.LogError("Tried to open object, but object is neither issue nor pull request");
        }
    }

    /// <summary>
    /// Updates an issue or pull request with a new body.
    /// </summary>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <param name="body">The new body of the issue or pull request.</param>
    /// <returns>An empty task.</returns>
    public static async Task UpdateBody(InstanceData data, string body)
    {
        data.Logger.LogInformation($"GitHub: Update Body");

        if (data.HasPullRequestData)
        {
            PullRequestUpdate updatedPR = new()
            {
                Base = data.PullRequest.Base.Ref,
                Body = body,
                Title = data.PullRequest.Title,
                State = ItemState.Open
            };

            data.IssuePrBody = body;

            if (!data.IsDryRun)
            {
                data.PullRequest = await data.GitHubRESTClient.PullRequest.Update(data.RepositoryId, data.PullRequest.Number, updatedPR);
                data.IssuePrBody = data.PullRequest.Body;
            }
        }
        else if (data.HasIssueData)
        {
            IssueUpdate updatedIssue = data.Issue.ToUpdate();
            updatedIssue.Body = body;
            data.IssuePrBody = body;

            if (!data.IsDryRun)
            {
                data.Issue = await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updatedIssue);
                data.IssuePrBody = data.Issue.Body;
            }
        }
        else
        {
            data.Logger.LogError("Tried to open object, but object is neither issue nor pull request");
        }
    }
}

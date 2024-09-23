using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class Milestones
{
    /// <summary>
    /// Caches and returns a list of all milestones associated with the repository.
    /// </summary>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>A read-only collection of milestones.</returns>
    public static async Task<IReadOnlyList<Milestone>> GetMilestones(InstanceData data)
    {
        if (data.Milestones != null) return data.Milestones;

        data.Logger.LogInformation($"GitHub: Get milestones");
        data.Milestones = await data.GitHubRESTClient.Issue.Milestone.GetAllForRepository(data.RepositoryId);

        return data.Milestones;
    }

    /// <summary>
    /// Assigns a milestone to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="milestone">The milestone id.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task SetMilestone(int milestone, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add milestone to an object that doesn't have issue data");
            return;
        }

        IssueUpdate updateIssue = data.Issue.ToUpdate();
        updateIssue.Milestone = milestone;
        data.Logger.LogInformation("GitHub: Set milestone to {milestone}", milestone);
        if (!data.IsDryRun)
            await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updateIssue);
    }

    /// <summary>
    /// Removes the milestone.
    /// </summary>
    /// <param name="milestone">The milestone id.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>An empty task.</returns>
    public static async Task RemoveMilestone(InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to remove milestone from an object that doesn't have issue data");
            return;
        }

        IssueUpdate updateIssue = data.Issue.ToUpdate();
        updateIssue.Milestone = null;
        data.Logger.LogInformation($"GitHub: Clearing milestone");
        if (!data.IsDryRun)
            await data.GitHubRESTClient.Issue.Update(data.RepositoryId, data.Issue.Number, updateIssue);
    }
}

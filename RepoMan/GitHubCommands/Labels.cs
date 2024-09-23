using Microsoft.Extensions.Logging;
using Octokit;

namespace DotNetDocs.RepoMan.GitHubCommands;

internal static class Labels
{
    /// <summary>
    /// Adds the specified labels to the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="labels">A list of labels to add.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>A task.</returns>
    public static async Task AddLabels(string[] labels, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add labels to an object that doesn't have issue data");
            return;
        }

        if (labels.Length != 0)
        {
            data.Logger.LogInformation("GitHub: Labels added: {labels}", string.Join(",", labels));
            if (!data.IsDryRun)
                await data.GitHubRESTClient.Issue.Labels.AddToIssue(data.RepositoryId, data.Issue.Number, labels.Select(data.ExpandVariables).ToArray());
        }
        else
            data.Logger.LogTrace("No labels to add");
    }

    /// <summary>
    /// Removes labels from the provided <see cref="State.Issue"/>.
    /// </summary>
    /// <param name="labels">An array of labels to remove from the issue.</param>
    /// <param name="existingLabels">Labels from the issue.</param>
    /// <param name="data">The state object of the Azure Function.</param>
    /// <returns>A task.</returns>
    public static async Task RemoveLabels(string[] labels, IReadOnlyList<Label> existingLabels, InstanceData data)
    {
        if (data.HasIssueData is false)
        {
            data.Logger.LogError("Tried to add labels to an object that doesn't have issue data.");
            return;
        }

        IEnumerable<string> existingLabelsTransformed = existingLabels.Select(l => l.Name.ToLower());

        if (labels.Length != 0 && existingLabels != null && existingLabels.Count != 0)
        {
            List<string> removedLabels = [];

            foreach (string label in labels)
                if (existingLabelsTransformed.Contains(data.ExpandVariables(label).ToLower()))
                    removedLabels.Add(label);

            data.Logger.LogInformation("GitHub: Labels removed: {labels}", string.Join(",", labels));

            foreach (string label in removedLabels)
                if (!data.IsDryRun)
                    await data.GitHubRESTClient.Issue.Labels.RemoveFromIssue(data.RepositoryId, data.Issue.Number, label);
        }
        else
            data.Logger.LogTrace("No labels to remove");
    }
}

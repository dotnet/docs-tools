namespace Quest2GitHub;

/// <summary>
/// This class manages the top level workflows to synchronize
/// GitHub issues with Quest work items (typically User Stories).
/// </summary>
/// <remarks>
/// Client applications should create an insteance of this class at startup.
/// </remarks>
public class QuestGitHubService : IDisposable
{
    private const string LinkedWorkItemComment = "Associated WorkItem - ";

    private readonly GitHubClient _ghClient;
    private readonly QuestClient _azdoClient;
    private readonly OspoClient _ospoClient;
    private readonly string _areaPath;
    private readonly string _questLinkString;
    private readonly string _importTriggerLabelText;
    private readonly string _importedLabelText;

    private GitHubLabel? _importTriggerLabel;
    private GitHubLabel? _importedLabel;

    /// <summary>
    /// Initialize the service.
    /// </summary>
    /// <param name="ghKey">GitHub personal access token</param>
    /// <param name="ospoKey">MS Open Source Programs Office personal access token</param>
    /// <param name="azdoKey">Azure Dev Ops personal access token</param>
    /// <param name="questOrg">The Azure Dev ops organization</param>
    /// <param name="questProject">The Azure Dev ops project</param>
    /// <param name="areaPath">The area path for work items from this repo</param>
    /// <param name="importTriggerLabel">The text of the label that triggers an import</param>
    /// <param name="importedLabel">The text of the label that indicates an issue has been imported</param>
    public QuestGitHubService(
        string ghKey,
        string ospoKey,
        string azdoKey,
        string questOrg,
        string questProject,
        string areaPath,
        string importTriggerLabel,
        string importedLabel)
    {
        _ghClient = new GitHubClient(ghKey);
        _ospoClient = new OspoClient(ospoKey);
        _azdoClient = new QuestClient(azdoKey, questOrg, questProject);
        _areaPath = areaPath;
        _questLinkString = $"https://dev.azure.com/{questOrg}/{questProject}/_workitems/edit/";
        _importTriggerLabelText = importTriggerLabel;
        _importedLabelText = importedLabel;
    }

    /// <summary>
    /// Process all open issues in a repository
    /// </summary>
    /// <param name="organization">The GitHub org</param>
    /// <param name="repository">The GitHub repository</param>
    /// <param name="dryRun">true for a dry run, false to process all issues</param>
    /// <returns></returns>
    public async Task ProcessIssues(string organization, string repository, bool dryRun)
    {
        if ((_importTriggerLabel == null) || (_importedLabel == null))
            await retrieveLabelIDs(organization, repository);

        var iter = new EnumerateIssues();

        int totalImport = 0;
        int totalSkipped = 0;
        await foreach (var item in iter.AllOpenIssue(_ghClient, organization, repository))
        {
            if (item.Labels.Any(l => (l.nodeID == _importTriggerLabel?.nodeID) || (l.nodeID == _importedLabel?.nodeID)))
            {
                Console.WriteLine($"{item.IssueNumber}: {item.Title}");
                var questItem = await FindLinkedWorkItem(item);
                if (questItem != null)
                {
                    await UpdateWorkItem(questItem, item);
                }
                else
                {
                    questItem = await LinkIssue(organization, repository, item);
                }

                totalImport++;
            }
            else
            {
                totalSkipped++;
                Console.WriteLine($"{item.IssueNumber}: skipped");
            }
        }
        Console.WriteLine($"Imported {totalImport} issues. Skipped {totalSkipped}");
    }

    /// <summary>
    /// Process one single issue
    /// </summary>
    /// <param name="gitHubOrganization">The GitHub organization</param>
    /// <param name="gitHubRepository">The GitHub repository</param>
    /// <param name="issueNumber">The issue Number</param>
    /// <returns></returns>
    public async Task ProcessIssue(string gitHubOrganization, string gitHubRepository, int issueNumber)
    {
        if ((_importTriggerLabel == null) || (_importedLabel == null))
            await retrieveLabelIDs(gitHubOrganization, gitHubRepository);

        //Retrieve the GitHub issue.
         var ghIssue = await RetrieveIssueAsync(gitHubOrganization, gitHubRepository, issueNumber);
        // Find a possible linked item.
        var questItem = await FindLinkedWorkItem(ghIssue);

        if ((questItem != null) && (ghIssue.Labels.Any(l => l.nodeID == _importedLabel?.nodeID)))
        {
            await UpdateWorkItem(questItem, ghIssue);
        }
        else if (ghIssue.IsOpen && ghIssue.Labels.Any(l => l.nodeID == _importTriggerLabel?.nodeID))
        {
            questItem = await LinkIssue(gitHubOrganization, gitHubRepository, ghIssue);
        }
    }

    /// <summary>
    /// Dispose the clients for HTTP services.
    /// </summary>
    public void Dispose()
    {
        _ghClient?.Dispose();
        _azdoClient?.Dispose();
        _ospoClient?.Dispose();
    }


    private Task<GithubIssue> RetrieveIssueAsync(string org, string repo, int issueNumber) =>
            GithubIssue.QueryIssue(_ghClient, org, repo, issueNumber);

    private async Task<QuestWorkItem?> LinkIssue(string organization, string repo, GithubIssue ghIssue)
    {
        var workItem = LinkedQuestId(ghIssue);
        if (workItem == null)
        {
            // Create work item:
            var questItem = await QuestWorkItem.CreateWorkItemAsync(ghIssue, _azdoClient, _ospoClient, _areaPath, _importTriggerLabel?.nodeID);

            // Add Tagged comment to GH Issue description.
            var updatedBody = $"""
            {ghIssue.Body}


            ---
            [Associated WorkItem - {questItem.Id}]({_questLinkString}{questItem.Id})
            """;

            // Now, do the label remove and add:
            var mutation = new AddOrRemoveLabelMutation(_ghClient, ghIssue.Id);

            await mutation.PerformMutation(updatedBody, _importedLabel?.nodeID, _importTriggerLabel?.nodeID);
            return questItem;
        } else
        {
            throw new InvalidOperationException("Issue already linked");
        }
    }

    private async Task retrieveLabelIDs(string org, string repo)
    {
        var labelQuery = new EnumerateLabels(_ghClient, org, repo);
        await foreach (var label in labelQuery.AllLabels())
        {
            if (label.name == _importTriggerLabelText) _importTriggerLabel = label;
            if (label.name == _importedLabelText) _importedLabel = label;
        }
    }

    private async Task<QuestWorkItem?> UpdateWorkItem(QuestWorkItem questItem, GithubIssue ghIssue)
    {
        string? ghAssignee = await ghIssue.AssignedMicrosoftPreferredName(_ospoClient);
        List<JsonPatchDocument> patchDocument = new();
        JsonPatchDocument? assignPatch = default;
        if (ghAssignee != questItem.AssignedTo)
        {
            // build patch document for assignment.
            assignPatch = new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.AssignedTo",
                Value = ghAssignee ?? "",
            };
            patchDocument.Add(assignPatch);
        }
        var questItemOpen = questItem.State is not "Closed";
        if (ghIssue.IsOpen != questItemOpen)
        {
            // build patch document for state.
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.State",
                Value = ghIssue.IsOpen ? "Active" : "Closed",
            });
        }
        while (patchDocument.Any())
        {
            var jsonDocument = await _azdoClient.PatchWorkItem(questItem.Id, patchDocument);
            try
            {
                var newItem = QuestWorkItem.WorkItemFromJson(jsonDocument);
                return newItem;
            }
            catch (KeyNotFoundException)
            {
                // This will happen when the assignee IS a Microsoft FTE,
                // but that person isn't onboarded in Quest. (Likely a product team member, or CSE)

                // So, remove the node with the Assigned to and try again:
                if (!patchDocument.Remove(assignPatch!))
                {
                    // If there's no assign node, clear the doc and punt
                    patchDocument.Clear();
                }
            }
        }
        return null;
    }

    private async Task<QuestWorkItem?> FindLinkedWorkItem(GithubIssue issue)
    {
        int? questId = LinkedQuestId(issue);
        if (questId == null)
            return null;
        else
            return await QuestWorkItem.QueryWorkItem(_azdoClient, questId.Value);
    }

    private int? LinkedQuestId(GithubIssue issue)
    {
        if (issue.BodyHtml?.Contains(LinkedWorkItemComment) == true)
        {
            // The formatted HTML comment looks like:
            // <p dir="auto"><a href="https://dev.azure.com/{org}/{Project}/_workitems/edit/{ItemID}" rel="nofollow">Associated WorkItem - {ItemId}</a></p>

            int startIndex = issue.BodyHtml.IndexOf(LinkedWorkItemComment) + LinkedWorkItemComment.Length;
            int endIndex = issue.BodyHtml.IndexOf('<', startIndex);
            var idStr = issue.BodyHtml[startIndex..endIndex];
            return int.Parse(idStr);
        }
        return null;
    }
}
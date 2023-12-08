using DotNet.DocsTools.GitHubObjects;

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

    private readonly IGitHubClient _ghClient;
    private readonly QuestClient _azdoClient;
    private readonly OspoClient _ospoClient;
    private readonly string _areaPath;
    private readonly string _questLinkString;
    private readonly string _importTriggerLabelText;
    private readonly string _importedLabelText;

    private GitHubLabel? _importTriggerLabel;
    private GitHubLabel? _importedLabel;
    private QuestIteration[]? _allIterations;

    /// <summary>
    /// Initialize the service.
    /// </summary>
    /// <param name="client">GitHub client</param>
    /// <param name="ospoKey">MS Open Source Programs Office personal access token</param>
    /// <param name="azdoKey">Azure Dev Ops personal access token</param>
    /// <param name="questOrg">The Azure Dev ops organization</param>
    /// <param name="questProject">The Azure Dev ops project</param>
    /// <param name="areaPath">The area path for work items from this repo</param>
    /// <param name="importTriggerLabel">The text of the label that triggers an import</param>
    /// <param name="importedLabel">The text of the label that indicates an issue has been imported</param>
    /// <param name="bulkImport">True if this run is doing a bulk import.</param>
    /// <remarks>
    /// The OAuth token takes precendence over the github token, if both are 
    /// present.
    /// </remarks>
    public QuestGitHubService(
        IGitHubClient client,
        string ospoKey,
        string azdoKey,
        string questOrg,
        string questProject,
        string areaPath,
        string importTriggerLabel,
        string importedLabel,
        bool bulkImport)
    {
        _ghClient = client;
        _ospoClient = new OspoClient(ospoKey, bulkImport);
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
    /// <param name="duration">How far back to examine.</param>
    /// <param name="dryRun">true for a dry run, false to process all issues</param>
    /// <returns></returns>
    public async Task ProcessIssues(string organization, string repository, int duration, bool dryRun)
    {
        // Trigger the OSPO bulk import before making any edits to any issue:
        await _ospoClient.GetAllAsync();

        if (_importTriggerLabel is null || _importedLabel is null)
        {
            await RetrieveLabelIdsAsync(organization, repository);
        }
        
        _allIterations ??= await RetrieveIterationLabelsAsync();

        var currentIteration = QuestIteration.CurrentIteration(_allIterations);

        var query = new EnumerationQuery<QuestIssue, QuestIssueVariables>(_ghClient);

        var historyThreshold = (duration == -1) ? DateTime.MinValue : DateTime.Now.AddDays(-duration);

        int totalImport = 0;
        int totalSkipped = 0;
        await foreach (var item in query.PerformQuery(new QuestIssueVariables(false, organization, repository, importTriggerLabelText: _importTriggerLabelText, importedLabelText: _importedLabelText)))
        {
            if (item.UpdatedAt < historyThreshold)
                break;

            if (item.Labels.Any(l => (l.Id == _importTriggerLabel?.Id) || (l.Id == _importedLabel?.Id)))
            {
                // Console.WriteLine($"{item.IssueNumber}: {item.Title}");
                Console.WriteLine(item);
                var questItem = await FindLinkedWorkItemAsync(item);
                if (dryRun is false && currentIteration is not null)
                {
                    if (questItem != null)
                    {
                        await UpdateWorkItemAsync(questItem, item, currentIteration, _allIterations);
                    }
                    else
                    {
                        questItem = await LinkIssueAsync(organization, repository, item, currentIteration, _allIterations);
                    }
                }
                totalImport++;
            }
            else
            {
                totalSkipped++;
                Console.WriteLine($"{item.Number}: skipped");
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
    /// <returns>A task representing the current operation</returns>
    public async Task ProcessIssue(string gitHubOrganization, string gitHubRepository, int issueNumber)
    {
        // Trigger the OSPO bulk import before making any edits to any issue:
        await _ospoClient.GetAllAsync();

        if (_importTriggerLabel is null || _importedLabel is null)
        {
            await RetrieveLabelIdsAsync(gitHubOrganization, gitHubRepository);
        }
        
        _allIterations ??= await RetrieveIterationLabelsAsync();

        var currentIteration = QuestIteration.CurrentIteration(_allIterations) 
            ?? throw new Exception("No current iteration found");

        //Retrieve the GitHub issue.
        var ghIssue = await RetrieveIssueAsync(gitHubOrganization, gitHubRepository, issueNumber);

        // Evaluate the labels to determine the right action.
        var request = ghIssue.Labels.Any(l => l.Id == _importTriggerLabel?.Id);
        var sequestered = ghIssue.Labels.Any(l => l.Id == _importedLabel?.Id);
        // Only query AzDo if needed:
        var questItem = (request || sequestered)
            ? await FindLinkedWorkItemAsync(ghIssue)
            : null;

        // The order here is important to avoid a race condition that causes
        // an issue to be triggered multiple times.
        // First, if an issue is open and the trigger label is added, link or 
        // update. Update is safe, because it will only update the quest issue's
        // state or assigned field. That can't trigger a new GH action run.
        if (request)
        {
            if (questItem is null)
            {
                questItem = await LinkIssueAsync(gitHubOrganization, gitHubRepository, ghIssue, currentIteration, _allIterations);
            }
            else if (questItem is not null)
            {
                // This allows a human to force a manual update: just add the trigger label.
                // Note that it updates even if the item is closed.
                await UpdateWorkItemAsync(questItem, ghIssue, currentIteration, _allIterations);

            }
            // Next, if the item is already linked, consider any updates.
            // It's important that adding the linked label is the last
            // mutation done in the linking process. That way, the GH Action
            // does get triggered again. The second trigger will check for any updates
            // a human made to assigned or state while the initial run was taking place.
        }
        else if (sequestered && questItem is not null)
        {
            await UpdateWorkItemAsync(questItem, ghIssue, currentIteration, _allIterations);
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


    private Task<QuestIssue> RetrieveIssueAsync(string org, string repo, int issueNumber)
    {
        var query = new ScalarQuery<QuestIssue, QuestIssueVariables>(_ghClient);
        return query.PerformQuery(new QuestIssueVariables(true, org, repo, issueNumber));
    }

    private async Task<QuestIteration[]> RetrieveIterationLabelsAsync()
    {
        var sprintPackets = await _azdoClient.RetrieveAllIterations();

        var iterations = new List<QuestIteration>();
        foreach (var sprintElement in sprintPackets.Descendent("value").EnumerateArray())
        {
            var id = sprintElement.GetProperty("id").GetGuid();
            var name = sprintElement.GetProperty("name").GetString();
            var path = sprintElement.GetProperty("path").GetString();
            if ((name is not null) && (path is not null))
            {
                iterations.Add(new QuestIteration()
                {
                    Id = id,
                    Name = name,
                    Path = path,
                });
            }
        }
        return iterations.ToArray();
    }


    private async Task<QuestWorkItem?> LinkIssueAsync(string organization, string repo, QuestIssue ghIssue, QuestIteration currentIteration, 
        IEnumerable<QuestIteration> allIterations)
    {
        var workItem = LinkedQuestId(ghIssue);
        if (workItem is null)
        {
            // Remove the trigger label before doing anything. That prevents
            // a race condition causing multiple imports:
            var mutation = new AddAndRemoveLabelMutation(_ghClient, ghIssue.Id);

            // Yes, this needs some later refactoring. This call won't update the description.
            await mutation.PerformMutation("ignored", null, _importTriggerLabel?.Id);

            // Create work item:
            var questItem = await QuestWorkItem.CreateWorkItemAsync(ghIssue, _azdoClient, _ospoClient, _areaPath, _importTriggerLabel?.Id, currentIteration, allIterations);

            // Add Tagged comment to GH Issue description.
            var updatedBody = $"""
            {ghIssue.Body}


            ---
            [Associated WorkItem - {questItem.Id}]({_questLinkString}{questItem.Id})
            """;

            // Now, update the body, and add the label:
            await mutation.PerformMutation(updatedBody, _importedLabel?.Id, null);
            return questItem;
        }
        else
        {
            throw new InvalidOperationException("Issue already linked");
        }
    }

    private async Task RetrieveLabelIdsAsync(string org, string repo)
    {
        var labelQuery = new EnumerateLabels(_ghClient, org, repo);
        await foreach (var label in labelQuery.AllLabels())
        {
            if (label.Name == _importTriggerLabelText) _importTriggerLabel = label;
            if (label.Name == _importedLabelText) _importedLabel = label;
        }
    }

    private async Task<QuestWorkItem?> UpdateWorkItemAsync(QuestWorkItem questItem, QuestIssue ghIssue, QuestIteration currentIteration,
        IEnumerable<QuestIteration> allIterations)
    {
        var ghAssigneeEmailAddress = await ghIssue.AssignedMicrosoftEmailAddress(_ospoClient);
        AzDoIdentity? questAssigneeID = default;
        if (ghAssigneeEmailAddress?.EndsWith("@microsoft.com") == true)
        {
            questAssigneeID = await _azdoClient.GetIDFromEmail(ghAssigneeEmailAddress);
        }
        List<JsonPatchDocument> patchDocument = new();
        JsonPatchDocument? assignPatch = default;
        if (questAssigneeID?.Id != questItem.AssignedToId)
        {
            // build patch document for assignment.
            assignPatch = (questAssigneeID is null) ?
                new JsonPatchDocument
                {
                    Operation = Op.Remove,
                    Path = "/fields/System.AssignedTo",
                } :
                new JsonPatchDocument
                {
                    Operation = Op.Add,
                    Path = "/fields/System.AssignedTo",
                    Value = questAssigneeID,
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

            // When the issue is opened or closed, 
            // update the description. That picks up any new
            // labels and comments.
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.Description",
                From = default,
                Value = QuestWorkItem.BuildDescriptionFromIssue(ghIssue, null)
            });
        }
        var iterationSize = ghIssue.LatestStoryPointSize();
        var iteration = iterationSize?.ProjectIteration(allIterations);
        if ((iteration is not null) && (iteration.Path != questItem.IterationPath))
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/System.IterationPath",
                Value = iteration.Path,
            });
        }
        if ((iterationSize?.QuestStoryPoint() is not null) && (iterationSize.QuestStoryPoint() != questItem.StoryPoints))
        {
            patchDocument.Add(new JsonPatchDocument
            {
                Operation = Op.Add,
                Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                Value = iterationSize.QuestStoryPoint(),
            });
        }
        QuestWorkItem? newItem = default;
        if (patchDocument.Any())
        {
            var jsonDocument = await _azdoClient.PatchWorkItem(questItem.Id, patchDocument);
            newItem = QuestWorkItem.WorkItemFromJson(jsonDocument);
        }
        if (!ghIssue.IsOpen && (ghIssue.ClosingPRUrl is not null))
        {
            newItem = await questItem.AddClosingPR(_azdoClient, ghIssue.ClosingPRUrl) ?? newItem;
        }
        return newItem;
    }

    private async Task<QuestWorkItem?> FindLinkedWorkItemAsync(QuestIssue issue)
    {
        int? questId = LinkedQuestId(issue);
        if (questId is null)
            return null;
        else
            return await QuestWorkItem.QueryWorkItem(_azdoClient, questId.Value);
    }

    private int? LinkedQuestId(QuestIssue issue)
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
using DotNet.DocsTools.GitHubObjects;
using DotNet.DocsTools.GraphQLQueries;

namespace Quest2GitHub;

/// <summary>
/// This class manages the top level workflows to synchronize
/// GitHub issues with Quest work items (typically User Stories).
/// </summary>
/// <remarks>
/// Client applications should create an instance of this class at startup.
/// </remarks>
/// <remarks>
/// Initialize the service.
/// </remarks>
/// <param name="ghClient">GitHub client</param>
/// <param name="ospoKey">MS Open Source Programs Office personal access token</param>
/// <param name="azdoKey">Azure Dev Ops personal access token</param>
/// <param name="questOrg">The Azure Dev ops organization</param>
/// <param name="questProject">The Azure Dev ops project</param>
/// <param name="areaPath">The area path for work items from this repo</param>
/// <param name="importTriggerLabelText">The text of the label that triggers an import</param>
/// <param name="importedLabelText">The text of the label that indicates an issue has been imported</param>
/// <param name="bulkImport">True if this run is doing a bulk import.</param>
/// <remarks>
/// The OAuth token takes precedence over the GitHub token, if both are 
/// present.
/// </remarks>
public class QuestGitHubService(
    IGitHubClient ghClient,
    string ospoKey,
    string azdoKey,
    string questOrg,
    string questProject,
    string areaPath,
    string importTriggerLabelText,
    string importedLabelText,
    bool bulkImport) : IDisposable
{
    private const string LinkedWorkItemComment = "Associated WorkItem - ";
    private readonly QuestClient _azdoClient = new(azdoKey, questOrg, questProject);
    private readonly OspoClient _ospoClient = new(ospoKey, bulkImport);
    private readonly string _questLinkString = $"https://dev.azure.com/{questOrg}/{questProject}/_workitems/edit/";

    private GitHubLabel? _importTriggerLabel;
    private GitHubLabel? _importedLabel;
    private QuestIteration[]? _allIterations;

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

        var issueQuery = new EnumerationQuery<QuestIssue, QuestIssueOrPullRequestVariables>(ghClient);
        var prQuery = new EnumerationQuery<QuestPullRequest, QuestIssueOrPullRequestVariables>(ghClient);

        DateTime historyThreshold = (duration == -1) ? DateTime.MinValue : DateTime.Now.AddDays(-duration);

        int totalImport = 0;
        int totalSkipped = 0;
        await foreach (QuestIssueOrPullRequest item in ConcatQueries(
            issueQuery.PerformQuery(new QuestIssueOrPullRequestVariables(organization, repository, importTriggerLabelText: importTriggerLabelText, importedLabelText: importedLabelText)),
            prQuery.PerformQuery(new QuestIssueOrPullRequestVariables(organization, repository, importTriggerLabelText: importTriggerLabelText, importedLabelText: importedLabelText))
        ))
        {
            if (item.Labels.Any(l => (l.Id == _importTriggerLabel?.Id) || (l.Id == _importedLabel?.Id)))
            {
                Console.WriteLine($"{item.Number}: {item.Title}, {item.LatestStoryPointSize()?.Month ?? "???"}-{(item.LatestStoryPointSize()?.CalendarYear)?.ToString() ?? "??"}");
                // Console.WriteLine(item);
                QuestWorkItem? questItem = await FindLinkedWorkItemAsync(item);
                if (dryRun is false && currentIteration is not null)
                {
                    if (questItem != null)
                    {
                        await UpdateWorkItemAsync(questItem, item, _allIterations);
                    }
                    else
                    {
                        questItem = await LinkIssueAsync(item, currentIteration, _allIterations);
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

        // This is a very general method, and could be moved into a library and utility class.
        async IAsyncEnumerable<QuestIssueOrPullRequest> ConcatQueries(IAsyncEnumerable<QuestIssueOrPullRequest> issues, IAsyncEnumerable<QuestIssueOrPullRequest> pullRequests)
        {
            await foreach (QuestIssueOrPullRequest issue in issues)
            {
                if (issue.UpdatedAt < historyThreshold)
                    break;
            
                yield return issue;
            }
            await foreach (QuestIssueOrPullRequest pr in pullRequests)
            {
                if (pr.UpdatedAt < historyThreshold)
                    break;
                yield return pr;
            }
        }
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

        QuestIteration currentIteration = QuestIteration.CurrentIteration(_allIterations)
            ?? throw new Exception("No current iteration found");

        //Retrieve the GitHub issue.
        QuestIssueOrPullRequest? ghIssue = null;
        try {
            ghIssue = await RetrieveIssueAsync(gitHubOrganization, gitHubRepository, issueNumber);
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine($"Issue not found, trying a Pull request");
        }
        ghIssue ??= await RetrievePullRequestAsync(gitHubOrganization, gitHubRepository, issueNumber);
        if (ghIssue is null)
        {
            throw new InvalidOperationException("Neither issue nor pull request found");
        }
        // Evaluate the labels to determine the right action.
        bool request = ghIssue.Labels.Any(l => l.Id == _importTriggerLabel?.Id);
        bool sequestered = ghIssue.Labels.Any(l => l.Id == _importedLabel?.Id);
        // Only query AzDo if needed:
        QuestWorkItem? questItem = (request || sequestered)
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
                questItem = await LinkIssueAsync(ghIssue, currentIteration, _allIterations);
            }
            else if (questItem is not null)
            {
                // This allows a human to force a manual update: just add the trigger label.
                // Note that it updates even if the item is closed.
                await UpdateWorkItemAsync(questItem, ghIssue, _allIterations);

            }
            // Next, if the item is already linked, consider any updates.
            // It's important that adding the linked label is the last
            // mutation done in the linking process. That way, the GH Action
            // does get triggered again. The second trigger will check for any updates
            // a human made to assigned or state while the initial run was taking place.
        }
        else if (sequestered && questItem is not null)
        {
            await UpdateWorkItemAsync(questItem, ghIssue, _allIterations);
        }
    }

    /// <summary>
    /// Dispose the clients for HTTP services.
    /// </summary>
    public void Dispose()
    {
        ghClient?.Dispose();
        _azdoClient?.Dispose();
        _ospoClient?.Dispose();
        GC.SuppressFinalize(this);
    }


    private Task<QuestIssue?> RetrieveIssueAsync(string org, string repo, int issueNumber)
    {
        var query = new ScalarQuery<QuestIssue, QuestIssueOrPullRequestVariables>(ghClient);
        return query.PerformQuery(new QuestIssueOrPullRequestVariables(org, repo, issueNumber));
    }

    private Task<QuestPullRequest?> RetrievePullRequestAsync(string org, string repo, int issueNumber)
    {
        var query = new ScalarQuery<QuestPullRequest, QuestIssueOrPullRequestVariables>(ghClient);
        return query.PerformQuery(new QuestIssueOrPullRequestVariables(org, repo, issueNumber));
    }
    private async Task<QuestIteration[]> RetrieveIterationLabelsAsync()
    {
        JsonElement sprintPackets = await _azdoClient.RetrieveAllIterations();

        var iterations = new List<QuestIteration>();
        foreach (JsonElement sprintElement in sprintPackets.Descendent("value").EnumerateArray())
        {
            Guid id = sprintElement.GetProperty("id").GetGuid();
            string? name = sprintElement.GetProperty("name").GetString();
            string? path = sprintElement.GetProperty("path").GetString();
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
        return [.. iterations];
    }


    private async Task<QuestWorkItem?> LinkIssueAsync(QuestIssueOrPullRequest issueOrPullRequest, QuestIteration currentIteration, IEnumerable<QuestIteration> allIterations)
    {
        int? workItem = LinkedQuestId(issueOrPullRequest);
        if (workItem is null)
        {
            // Create work item:
            QuestWorkItem questItem = await QuestWorkItem.CreateWorkItemAsync(issueOrPullRequest, _azdoClient, _ospoClient, areaPath, _importTriggerLabel?.Id, currentIteration, allIterations);

            string linkText = $"[{LinkedWorkItemComment}{questItem.Id}]({_questLinkString}{questItem.Id})";
            string updatedBody = $"""
               {issueOrPullRequest.Body}

               ---
               {linkText}
               """;

            if (issueOrPullRequest is QuestIssue issue)
            {
                var issueMutation = new Mutation<SequesteredIssueMutation, SequesterVariables>(ghClient);
                await issueMutation.PerformMutation(new SequesterVariables(issue.Id, _importTriggerLabel?.Id ?? "", _importedLabel?.Id ?? "", updatedBody));
            }
            else if (issueOrPullRequest is QuestPullRequest pr)
            {
                var prMutation = new Mutation<SequesteredPullRequestMutation, SequesterVariables>(ghClient);
                await prMutation.PerformMutation(new SequesterVariables(pr.Id, _importTriggerLabel?.Id ?? "", _importedLabel?.Id ?? "", updatedBody));
            }
            return questItem;
        }
        else
        {
            throw new InvalidOperationException("Issue already linked");
        }
    }

    private async Task RetrieveLabelIdsAsync(string org, string repo)
    {
        var labelQuery = new EnumerationQuery<GitHubLabel, FindLabelQueryVariables>(ghClient);
            
        await foreach (GitHubLabel label in labelQuery.PerformQuery(new FindLabelQueryVariables(org, repo, "")))
        {
            if (label.Name == importTriggerLabelText) _importTriggerLabel = label;
            if (label.Name == importedLabelText) _importedLabel = label;
        }
    }

    private async Task<QuestWorkItem?> UpdateWorkItemAsync(QuestWorkItem questItem, QuestIssueOrPullRequest ghIssue, IEnumerable<QuestIteration> allIterations)
    {
        string? ghAssigneeEmailAddress = await ghIssue.QueryAssignedMicrosoftEmailAddressAsync(_ospoClient);
        AzDoIdentity? questAssigneeID = default;
        if (ghAssigneeEmailAddress?.EndsWith("@microsoft.com") == true)
        {
            questAssigneeID = await _azdoClient.GetIDFromEmail(ghAssigneeEmailAddress);
        }
        List<JsonPatchDocument> patchDocument = [];
        if (questAssigneeID?.Id != questItem.AssignedToId)
        {
            // build patch document for assignment.
            JsonPatchDocument? assignPatch = (questAssigneeID is null) ?
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
        bool questItemOpen = questItem.State is not "Closed";
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
        StoryPointSize? iterationSize = ghIssue.LatestStoryPointSize();
        if (iterationSize != null)
        {
            Console.WriteLine($"Latest GitHub sprint project: {iterationSize?.Month}-{iterationSize?.CalendarYear}, size: {iterationSize?.Size}");
        } else
        {
            Console.WriteLine("No GitHub sprint project found - using current iteration.");
        }
        QuestIteration? iteration = iterationSize?.ProjectIteration(allIterations);
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
                From = default,
                Path = "/fields/Microsoft.VSTS.Scheduling.StoryPoints",
                Value = iterationSize.QuestStoryPoint(),
            });
        }
        QuestWorkItem? newItem = default;
        if (patchDocument.Count != 0)
        {
            JsonElement jsonDocument = await _azdoClient.PatchWorkItem(questItem.Id, patchDocument);
            newItem = QuestWorkItem.WorkItemFromJson(jsonDocument);
        }
        if (!ghIssue.IsOpen && (ghIssue.ClosingPRUrl is not null))
        {
            newItem = await questItem.AddClosingPR(_azdoClient, ghIssue.ClosingPRUrl) ?? newItem;
        }
        return newItem;
    }

    private async Task<QuestWorkItem?> FindLinkedWorkItemAsync(QuestIssueOrPullRequest issue)
    {
        int? questId = LinkedQuestId(issue);
        if (questId is null)
            return null;
        else
            return await QuestWorkItem.QueryWorkItem(_azdoClient, questId.Value);
    }

    private static int? LinkedQuestId(QuestIssueOrPullRequest issue)
    {
        if (issue.BodyHtml?.Contains(LinkedWorkItemComment) == true)
        {
            // The formatted HTML comment looks like:
            // <p dir="auto"><a href="https://dev.azure.com/{org}/{Project}/_workitems/edit/{ItemID}" rel="nofollow">Associated WorkItem - {ItemId}</a></p>

            int startIndex = issue.BodyHtml.IndexOf(LinkedWorkItemComment) + LinkedWorkItemComment.Length;
            int endIndex = issue.BodyHtml.IndexOf('<', startIndex);
            string idStr = issue.BodyHtml[startIndex..endIndex];
            return int.Parse(idStr);
        }
        return null;
    }
}

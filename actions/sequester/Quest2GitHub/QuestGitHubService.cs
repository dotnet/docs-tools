using System.Xml.XPath;
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
/// <param name="ospoClient">MS Open Source Programs Office client</param>
/// <param name="azdoKey">Azure Dev Ops personal access token</param>
/// <param name="questOrg">The Azure Dev ops organization</param>
/// <param name="questProject">The Azure Dev ops project</param>
/// <param name="areaPath">The area path for work items from this repo</param>
/// <param name="importTriggerLabelText">The text of the label that triggers an import</param>
/// <param name="importedLabelText">The text of the label that indicates an issue has been imported</param>
/// <param name="parentNodes">A dictionary of label / parent ID pairs.</param>
/// <remarks>
/// The OAuth token takes precedence over the GitHub token, if both are 
/// present.
/// </remarks>
public class QuestGitHubService(
    IGitHubClient ghClient,
    OspoClient? ospoClient,
    string azdoKey,
    string questOrg,
    string questProject,
    string areaPath,
    string importTriggerLabelText,
    string importedLabelText,
    List<ParentForLabel> parentNodes,
    IEnumerable<LabelToTagMap> tagMap) : IDisposable
{
    private const string LinkedWorkItemComment = "Associated WorkItem - ";
    private readonly QuestClient _azdoClient = new(azdoKey, questOrg, questProject);
    private readonly OspoClient? _ospoClient = ospoClient;
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
    /// <returns></returns>
    public async Task ProcessIssues(string organization, string repository, int duration)
    {
        if (_importTriggerLabel is null || _importedLabel is null)
        {
            await RetrieveLabelIdsAsync(organization, repository);
        }
        
        _allIterations ??= await RetrieveIterationLabelsAsync();

        var currentIteration = QuestIteration.CurrentIteration(_allIterations);

        DateTime historyThreshold = (duration == -1) ? DateTime.MinValue : DateTime.Now.AddDays(-duration);
        int totalImport = 0;
        int totalSkipped = 0;

        Console.WriteLine("-----   Starting processing issues.          --------");
        var issueQueryEnumerable = (duration == -1) ?
            QueryAllOpenIssuesOrPullRequests<QuestIssue>() :
            QueryIssuesOrPullRequests<QuestIssue>();
        await ProcessItems(issueQueryEnumerable);
        Console.WriteLine("-----   Finished processing issues.          --------");
        Console.WriteLine("     ----- Regenerating bearer token   ------");
        await ghClient.RegenerateBearerToken();
        try
        {
            Console.WriteLine("-----   Starting processing pull requests.   --------");
            var prQueryEnumerable = (duration == -1) ?
            QueryAllOpenIssuesOrPullRequests<QuestPullRequest>() :
                QueryIssuesOrPullRequests<QuestPullRequest>();
            await ProcessItems(prQueryEnumerable);
            Console.WriteLine("-----   Finished processing pull requests.   --------");
        } catch (InvalidOperationException e)
        {
            Console.WriteLine("Warning: GitHub query for Pull Requests failed.");
            Console.WriteLine(e);
        }
        Console.WriteLine($"Imported {totalImport} issues. Skipped {totalSkipped}");

        async Task ProcessItems(IAsyncEnumerable<QuestIssueOrPullRequest> items)
        {
            await foreach (QuestIssueOrPullRequest item in items)
            {
                if (item.Labels.Any(l => (l.Id == _importTriggerLabel?.Id) || (l.Id == _importedLabel?.Id)))
                {
                    var issueProperties = new WorkItemProperties(item, _allIterations, tagMap, parentNodes);

                    Console.WriteLine($"{item.Number}: {item.Title}, {issueProperties.IssueLogString}");
                    // Console.WriteLine(item);
                    QuestWorkItem? questItem = await FindLinkedWorkItemAsync(item);
                    if (questItem != null)
                    {
                        await QuestWorkItem.UpdateWorkItemAsync(questItem, item, _azdoClient, _ospoClient, issueProperties);
                    }
                    else
                    {
                        questItem = await LinkIssueAsync(item, issueProperties);
                        // Because some fields can't be set in the initial creation, update the item.
                        await QuestWorkItem.UpdateWorkItemAsync(questItem, item, _azdoClient, _ospoClient, issueProperties);
                    }
                    totalImport++;
                }
                else
                {
                    totalSkipped++;
                    Console.WriteLine($"{item.Number}: skipped");
                }
            }
        }

        async IAsyncEnumerable<QuestIssueOrPullRequest> QueryIssuesOrPullRequests<T>() where T : QuestIssueOrPullRequest, IGitHubQueryResult<T, QuestIssueOrPullRequestVariables>
        {
            var query = new EnumerationQuery<T, QuestIssueOrPullRequestVariables>(ghClient);
            var queryEnumerable = query.PerformQuery(new QuestIssueOrPullRequestVariables(organization, repository, [], importTriggerLabelText: importTriggerLabelText, importedLabelText: importedLabelText));
            await foreach (QuestIssueOrPullRequest item in queryEnumerable)
            {
                if (item.UpdatedAt < historyThreshold)
                    break;

                yield return item;
            }
        }

        async IAsyncEnumerable<QuestIssueOrPullRequest> QueryAllOpenIssuesOrPullRequests<T>() where T : QuestIssueOrPullRequest, IGitHubQueryResult<T, QuestIssueOrPullRequestVariables>
        {
            var query = new EnumerationQuery<T, QuestIssueOrPullRequestVariables>(ghClient);
            var queryEnumerable = query.PerformQuery(new QuestIssueOrPullRequestVariables(organization, repository, ["OPEN"], importTriggerLabelText: importTriggerLabelText, importedLabelText: importedLabelText));
            await foreach (QuestIssueOrPullRequest item in queryEnumerable)
            {
                yield return item;
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
        if (_importTriggerLabel is null || _importedLabel is null)
        {
            await RetrieveLabelIdsAsync(gitHubOrganization, gitHubRepository);
        }
        
        _allIterations ??= await RetrieveIterationLabelsAsync();

        QuestIteration currentIteration = QuestIteration.CurrentIteration(_allIterations);

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

        var issueProperties = new WorkItemProperties(ghIssue, _allIterations, tagMap, parentNodes);

        // The order here is important to avoid a race condition that causes
        // an issue to be triggered multiple times.
        // First, if an issue is open and the trigger label is added, link or 
        // update. Update is safe, because it will only update the quest issue's
        // state or assigned field. That can't trigger a new GH action run.
        if (request)
        {
            if (questItem is null)
            {
                questItem = await LinkIssueAsync(ghIssue, issueProperties);
            }
            else if (questItem is not null)
            {
                // This allows a human to force a manual update: just add the trigger label.
                // Note that it updates even if the item is closed.
                await QuestWorkItem.UpdateWorkItemAsync(questItem, ghIssue, _azdoClient, _ospoClient, issueProperties);

            }
            // Next, if the item is already linked, consider any updates.
            // It's important that adding the linked label is the last
            // mutation done in the linking process. That way, the GH Action
            // does get triggered again. The second trigger will check for any updates
            // a human made to assigned or state while the initial run was taking place.
        }
        else if (sequestered && questItem is not null)
        {
            await QuestWorkItem.UpdateWorkItemAsync(questItem, ghIssue, _azdoClient, _ospoClient, issueProperties);
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
        return query.PerformQuery(new QuestIssueOrPullRequestVariables(org, repo, [], issueNumber));
    }

    private Task<QuestPullRequest?> RetrievePullRequestAsync(string org, string repo, int issueNumber)
    {
        var query = new ScalarQuery<QuestPullRequest, QuestIssueOrPullRequestVariables>(ghClient);
        return query.PerformQuery(new QuestIssueOrPullRequestVariables(org, repo, [], issueNumber));
    }
    private async Task<QuestIteration[]> RetrieveIterationLabelsAsync()
    {
        JsonElement sprintPackets = await _azdoClient.RetrieveAllIterations();

        var parentIteration = sprintPackets.Descendent("value").EnumerateArray().Single(i => i.GetProperty("structureType").GetString() == "iteration");


        return [.. ChildIterations(parentIteration)];

        static IEnumerable<QuestIteration> ChildIterations(JsonElement parentIteration)
        {
            foreach (JsonElement sprintElement in parentIteration.Descendent("children").EnumerateArray())
            {
                if (sprintElement.TryGetProperty("children", out JsonElement children))
                {
                    foreach (var child in ChildIterations(sprintElement))
                    {
                        yield return child;
                    }
                }
                else if (sprintElement.GetProperty("hasChildren").GetBoolean() == false)
                {
                    var iteration = ConstructIteration(sprintElement);
                    if (iteration is not null)
                    {
                        yield return iteration;
                    }
                }
            }
        }

        static QuestIteration? ConstructIteration(JsonElement sprintElement)
        {
            int id = sprintElement.GetProperty("id").GetInt32();
            Guid identifier = sprintElement.GetProperty("identifier").GetGuid();
            string? name = sprintElement.GetProperty("name").GetString();
            string? path = sprintElement.GetProperty("path").GetString()
                ?.Replace("\\Content\\Iteration", "Content");
            if ((name is not null) && (path is not null))
            {
                return new QuestIteration()
                {
                    Id = id,
                    Identifier = identifier,
                    Name = name,
                    Path = path,
                };
            }
            return null;
        }
    }

    private async Task<QuestWorkItem> LinkIssueAsync(QuestIssueOrPullRequest issueOrPullRequest, WorkItemProperties issueProperties)
    {
        int? workItem = LinkedQuestId(issueOrPullRequest);
        if (workItem is null)
        {
            // Create work item:
            QuestWorkItem questItem = await QuestWorkItem.CreateWorkItemAsync(issueOrPullRequest, _azdoClient, _ospoClient, areaPath,
                _importTriggerLabel?.Id, issueProperties);

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

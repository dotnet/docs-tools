using System.Xml.XPath;
using DotNet.DocsTools.GitHubObjects;
using DotNet.DocsTools.GraphQLQueries;
using Org.BouncyCastle.Asn1.Ocsp;

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
/// <param name="importOptions">A structure with the options for this run.</param>
/// <remarks>
/// The OAuth token takes precedence over the GitHub token, if both are 
/// present.
/// </remarks>
public class QuestGitHubService(
    IGitHubClient ghClient,
    OspoClient? ospoClient,
    ImportOptions importOptions) : IDisposable
{
    private const string LinkedWorkItemComment = "Associated WorkItem - ";
    private readonly QuestClient _azdoClient = new(importOptions.ApiKeys.QuestKey, importOptions.AzureDevOps.Org, importOptions.AzureDevOps.Project);
    private readonly OspoClient? _ospoClient = ospoClient;
    private readonly string _questLinkString = $"https://dev.azure.com/{importOptions.AzureDevOps.Org}/{importOptions.AzureDevOps.Project}/_workitems/edit/";

    private GitHubLabel? _importTriggerLabel;
    private GitHubLabel? _importedLabel;
    private GitHubLabel? _removeLinkedItemLabel;
    private GitHubLabel? _locItemLabel;
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
        if (_importTriggerLabel is null || _importedLabel is null  || _removeLinkedItemLabel is null || _locItemLabel is null)
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
        Console.WriteLine($"Imported {totalImport} issues. Skipped {totalSkipped}");

        async Task ProcessItems(IAsyncEnumerable<QuestIssueOrPullRequest> items)
        {
            await foreach (QuestIssueOrPullRequest item in items)
            {
                if (item.Labels.Any(l => (l.Id == _importTriggerLabel?.Id) || (l.Id == _importedLabel?.Id) || (l.Id == _removeLinkedItemLabel?.Id)))
                {
                    bool request = item.Labels.Any(l => l.Id == _importTriggerLabel?.Id);
                    bool sequestered = item.Labels.Any(l => l.Id == _importedLabel?.Id);
                    bool vanquished = item.Labels.Any(l => l.Id == _removeLinkedItemLabel?.Id);
                    bool localization = item.Labels.Any(l => l.Id == _locItemLabel?.Id);
                    // Only query AzDo if needed:
                    QuestWorkItem? questItem = (request || sequestered || vanquished || localization)
                        ? await FindLinkedWorkItemAsync(item)
                        : null;
                    var issueProperties = new WorkItemProperties(item, _allIterations, importOptions.WorkItemTags, importOptions.ParentNodes, importOptions.DefaultParentNode, importOptions.CopilotIssueTag);

                    Console.WriteLine($"{item.Number}: {item.Title}, {issueProperties.IssueLogString}");
                    Task workDone = (request, sequestered, vanquished, localization, questItem) switch
                    {
                        (_, _, _, true, null) => ImportLocalizationItemAsync(item, issueProperties), // No link, but one of the link labels was applied.
                        (false, false, false, _,  _) => Task.CompletedTask, // No labels. Do nothing.
                        (_, _, true, _, null) => Task.CompletedTask, // Unlink, but no link. Do nothing.
                        (_, _, false, _, null) => LinkIssueAsync(item, issueProperties), // No link, but one of the link labels was applied.
                        (_, _, true, _, not null) => questItem.RemoveWorkItem(item, _azdoClient, issueProperties), // Unlink.
                        (_, _, false, _, not null) => questItem.UpdateWorkItemAsync(item, _azdoClient, _ospoClient, importOptions.TeamGitHubLogins, issueProperties), // update
                    };
                    totalImport++;
                    await workDone;
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
            var queryEnumerable = query.PerformQuery(new QuestIssueOrPullRequestVariables(
                organization, repository, [],
                importTriggerLabelText: importOptions.ImportTriggerLabel,
                importedLabelText: importOptions.ImportedLabel,
                removeLabelText:  importOptions.UnlinkLabel,
                localizationLabelText: importOptions.LocalizationLabel));
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
            var queryEnumerable = query.PerformQuery(new QuestIssueOrPullRequestVariables(organization, repository, ["OPEN"],
                importTriggerLabelText: importOptions.ImportTriggerLabel,
                importedLabelText: importOptions.ImportedLabel,
                removeLabelText: importOptions.UnlinkLabel,
                localizationLabelText: importOptions.LocalizationLabel));
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
        if (_importTriggerLabel is null || _importedLabel is null || _removeLinkedItemLabel is null)
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
        bool vanquished = ghIssue.Labels.Any(l => l.Id == _removeLinkedItemLabel?.Id);
        bool localization = ghIssue.Labels.Any(l => l.Id == _locItemLabel?.Id);
        // Only query AzDo if needed:
        QuestWorkItem? questItem = (request || sequestered || vanquished)
            ? await FindLinkedWorkItemAsync(ghIssue)
            : null;

        var issueProperties = new WorkItemProperties(ghIssue, _allIterations, importOptions.WorkItemTags, importOptions.ParentNodes, importOptions.DefaultParentNode, importOptions.CopilotIssueTag);

        Task workDone = (request, sequestered, vanquished, localization, questItem) switch
        {
            (_, _, _, true, null) => ImportLocalizationItemAsync(ghIssue, issueProperties), // Localization issue
            (false, false, false, _, _) => Task.CompletedTask, // No labels. Do nothing.
            (    _,     _,  true, _, null) => Task.CompletedTask, // Unlink, but no link. Do nothing.
            (    _,     _, false, _, null) => LinkIssueAsync(ghIssue, issueProperties), // No link, but one of the link labels was applied.
            (    _,     _,  true, _, not null) => questItem.RemoveWorkItem(ghIssue, _azdoClient, issueProperties), // Unlink.
            (    _,     _, false, _, not null) => questItem.UpdateWorkItemAsync(ghIssue, _azdoClient, _ospoClient, importOptions.TeamGitHubLogins, issueProperties), // update
        };
        await workDone;
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
                else
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
            QuestWorkItem questItem = await QuestWorkItem.CreateWorkItemAsync(issueOrPullRequest, _azdoClient, importOptions.AzureDevOps.AreaPath,
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

            // Because some fields can't be set when an item is created, go through an update cycle:
            await questItem.UpdateWorkItemAsync(issueOrPullRequest, _azdoClient, _ospoClient, importOptions.TeamGitHubLogins, issueProperties);
            return questItem;
        }
        else
        {
            throw new InvalidOperationException("Issue already linked");
        }
    }

    private async Task<QuestWorkItem> ImportLocalizationItemAsync(QuestIssueOrPullRequest issueOrPullRequest, WorkItemProperties issueProperties)
    {
        int? workItem = LinkedQuestId(issueOrPullRequest);
        if (workItem is null)
        {
            // Create work item:
            QuestWorkItem questItem = await QuestWorkItem.CreateWorkItemAsync(issueOrPullRequest, _azdoClient, importOptions.AzureDevOps.AreaPath,
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

            // Because some fields can't be set when an item is created, go through an update cycle:
            await questItem.UpdateWorkItemAsync(issueOrPullRequest, _azdoClient, _ospoClient, importOptions.TeamGitHubLogins, issueProperties);
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
            if (label.Name == importOptions.ImportTriggerLabel) _importTriggerLabel = label;
            if (label.Name == importOptions.ImportedLabel) _importedLabel = label;
            if (label.Name == importOptions.UnlinkLabel) _removeLinkedItemLabel = label;
            if (label.Name == importOptions.LocalizationLabel) _locItemLabel = label;
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

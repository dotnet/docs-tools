using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;
using DotNet.DocsTools.GitHubObjects;

namespace IssueCloser;

class Program
{
    private const string ConfigFile = "bulkcloseconfig.json";
    // shoag, rpetrusha worked with us, but have retired:
    private static readonly string[] teamAuthors = new string[] { "shoag", "rpetrusha" };

    // The text of the comment to add:
    private static string commentText =
@"This issue has been closed as part of the issue backlog grooming process outlined in #22351.

That automated process may have closed some issues that should be addressed. If you think this is one of them, reopen it with a comment explaining why. Tag the `@dotnet/docs` team for visibility.";

    /// <summary>
    /// Close issues based on age, author (customer or MS employee) and priority labels.
    /// This was written for (hopefully) a one-time situation. The dotnet/docs repo
    /// reached over 1500 issues. We couldn't plan effectively, and we bulk closed
    /// a number of issues
    /// The tool search issues for candidates based on age, if the author is a customer
    /// or MS employee, and priority labels. Issues that are over the threshold are closed. 
    /// In addition, those issues have a comment pointing to a master issue that describes
    /// the process.
    /// </summary>
    /// <param name="organization">The organization managing the repo.</param>
    /// <param name="repository">The repository to update.</param>
    /// <param name="dryRun">True to list work, but not actually close any issue.</param>
    /// <returns>0 on success, a non-zero number on error conditions.</returns>
    static async Task<int> Main(string organization = "dotnet", string repository = "docs", bool dryRun = false)
    {
        var key = CommandLineUtility.GetEnvVariable("GitHubBotKey",
        "You must store the bot's GitHub key in the 'GitHubBotKey' environment variable",
        "");
        var ospoKey = CommandLineUtility.GetEnvVariable("OspoKey",
        "You must store your OSPO key in the 'OspoKey' environment variable",
        "");

        var client = IGitHubClient.CreateGitHubClient(key);
        var ospoClient = new OspoClient(ospoKey, true);

        var labelQuery = new ScalarQuery<GitHubLabel, FindLabelQueryVariables>(client);

        var label = await labelQuery.PerformQuery(new FindLabelQueryVariables(organization, repository, "won't fix"));
        if (label is null)
        {
            Console.WriteLine($"Could not find label [won't fix]");
            return -1;
        }
        var labelID = label.Id;

        try
        {
            // Next, starting paging through all issues:
            Console.WriteLine("Processing open issues");
            await ProcessIssues(client, ospoClient, organization, repository, dryRun, labelID);
            return 0;
        }
        catch (InvalidOperationException e)
        {
            Console.WriteLine(e.Message);
            return -1;
        }
    }

    private static async Task ProcessIssues(IGitHubClient client, OspoClient ospoClient, string organization, string repository, bool dryRun, string labelID)
    {
        var query = new EnumerationQuery<BankruptcyIssue, BankruptcyIssueVariables>(client);
        var now = DateTime.Now;

        var stats = await BuildStatsMapAsync();

        int totalClosedIssues = 0;
        int totalIssues = 0;
        await foreach (var item in query.PerformQuery(new BankruptcyIssueVariables(organization, repository)))
        {
            var issueID = item.Id;

            var priority = Priorities.PriLabel(item.Labels);
            bool isInternal = await item.Author.IsMicrosoftFTE(ospoClient) == true;
            if (teamAuthors.Contains(item.Author?.Login))
                isInternal = true;
            bool isDocIssue = IsDocsIssue(item.Body);
            int ageInMonths = (int)(now - item.CreatedDate).TotalDays / 30;
            var criteria = new CloseCriteria(priority, isDocIssue, isInternal);
            var number = item.Number;
            var title = item.Title;

            totalIssues++;
            if (stats[criteria].ShouldCloseIssue(criteria, ageInMonths))
            {
                Console.WriteLine($"Recommend Closing [{number} - {title}]");
                Console.WriteLine($"\t{criteria}, {ageInMonths}");

                totalClosedIssues++;
                if (!dryRun)
                {
                    await CloseIssue(client, issueID, labelID);
                    Console.WriteLine($"!!!!! Issue  CLOSED {number}-{title} !!!!!");
                }
            }
        }

        foreach (var item in stats.Where(item => item.Value.TotalIssues > 0))
        {
            Console.WriteLine($"- {item.Key}:\n  - {item.Value}");
        }

        Console.WriteLine($"Closing {totalClosedIssues} of {totalIssues}");
    }

    private static async Task CloseIssue(IGitHubClient client, string issueID, string labelID)
    {
        // 1. Add label
        Console.WriteLine($"\tAdding [won't fix] label.");
        var addMutation = new AddOrRemoveLabelMutation(client, issueID, labelID, true);
        await addMutation.PerformMutation();

        // 2. Add comment: body, nodeID
        var comentMutation = new AddCommentMutation(client, issueID, commentText);
        await comentMutation.PerformMutation();

        // 3. Close issue: nodeID
        var closeMutation = new CloseIssueMutation(client, issueID);
        await closeMutation.PerformMutation();
    }

    private static async Task<Dictionary<CloseCriteria, IssueSet>> BuildStatsMapAsync()
    {
        using FileStream openStream = File.OpenRead(ConfigFile);
        List<BulkCloseConfig>? items = await JsonSerializer.DeserializeAsync<List<BulkCloseConfig>>(openStream);
        // Uncomment this to build the config file for the first time:
        Dictionary<CloseCriteria, IssueSet> map = new Dictionary<CloseCriteria, IssueSet>();

        if (items != null)
        {
            foreach (var configItem in items)
            {
                map.Add(configItem.Criteria, new IssueSet { AgeToClose = configItem.AgeInMonths });
            }
        }
        else
        {
            items = new List<BulkCloseConfig>();
            int ageIndex = 0;
            foreach (var botPriority in System.Enum.GetValues<Priority>())
            {
                for (int codeBlock = 0; codeBlock < 2; codeBlock++)
                {
                    for (int author = 0; author < 2; author++)
                    {
                        var criteria = new CloseCriteria(botPriority, codeBlock == 0, author == 0);
                        int age = IssueSet.Ages[ageIndex++];
                        items.Add(new(criteria, age));
                        map.Add(criteria,
                            new IssueSet { AgeToClose = age });
                    }
                }
            }

            using FileStream createStream = File.Create(ConfigFile);
            await JsonSerializer.SerializeAsync(createStream, items, new JsonSerializerOptions() { WriteIndented = true });
        }
        return map;
    }
    private static bool IsDocsIssue(string? body)
    {
        const string header1 = "---";
        const string header2 = "#### ";
        const string header3 = "⚠ *";

        return (body != null) && body.Contains(header1) &&
            body.Contains(header2) &&
            body.Contains(header3);
    }
}

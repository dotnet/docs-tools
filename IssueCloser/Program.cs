using System.CommandLine;
using System.Text.Json;
using DotNet.DocsTools.GitHubObjects;
using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using DotNetDocs.Tools.Utility;
using Microsoft.DotnetOrg.Ospo;
using IssueCloser;
using System.CommandLine.Parsing;

const string ConfigFile = "bulkcloseconfig.json";

// The text of the comment to add:
const string commentText =
@"This issue has been closed as part of the issue backlog grooming process outlined in #22351.

That automated process may have closed some issues that should be addressed. If you think this is one of them, reopen it with a comment explaining why. Tag the `@dotnet/docs` team for visibility.";

var (organization, repository, dryRun) = ParseArguments(args);

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

static async Task ProcessIssues(IGitHubClient client, OspoClient ospoClient, string organization, string repository, bool dryRun, string labelID)
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

static async Task CloseIssue(IGitHubClient client, string issueID, string labelID)
{
    // 1. Add label
    Console.WriteLine($"\tAdding [won't fix] label.");
    Console.WriteLine($"\tAdding Closing comment.");
    Console.WriteLine($"\tClosing issue.");
    var closeIssueMutation = new Mutation<CloseIssueMutation, CloseIssueVariables>(client);
    await closeIssueMutation.PerformMutation(new CloseIssueVariables(issueID, labelID, commentText));
}

static async Task<Dictionary<CloseCriteria, IssueSet>> BuildStatsMapAsync()
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
static bool IsDocsIssue(string? body)
{
    const string header1 = "---";
    const string header2 = "#### ";
    const string header3 = "⚠ *";

    return (body != null) && body.Contains(header1) &&
        body.Contains(header2) &&
        body.Contains(header3);
}

static (string organization, string repository, bool dryRun) ParseArguments(string[] args)
{
    Option<string> organizationOption = new("--organization")
    {
        Description = "The GitHub organization for the target repository.",
        DefaultValueFactory = parseResult => "dotnet"
    };
    Option<string> repositoryOption = new("--repository")
    {
        Description = "The GitHub target repository.",
        DefaultValueFactory = parseResult => "docs"
    };
    Option<bool> dryRunOption = new("--dryRun")
    {
        Description = "Flag to specify a dry run (no issues will be closed).",
        DefaultValueFactory = parseResult => false
    };
    RootCommand rootCommand = new("Issue Closer application.");

    rootCommand.Options.Add(organizationOption);
    rootCommand.Options.Add(repositoryOption);
    rootCommand.Options.Add(dryRunOption);

    ParseResult result = rootCommand.Parse(args);
    foreach (ParseError parseError in result.Errors)
    {
        Console.Error.WriteLine(parseError.Message);
    }
    if (result.Errors.Count > 0)
    {
        throw new InvalidOperationException("Invalid command line.");
    }
    var organization = result.GetValue(organizationOption) ?? throw new InvalidOperationException("organization is null");
    var repository = result.GetValue(repositoryOption) ?? throw new InvalidOperationException("repository is null");
    var dryRun = result.GetValue(dryRunOption);
    return (organization, repository, dryRun);
}

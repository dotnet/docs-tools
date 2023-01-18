using Dotnet.DocsTools.GraphQLQueries;
using DotnetDocsTools.GitHubCommunications;
using GitHub.RepositoryExplorer.Models;

namespace GitHub.RepositoryExplorer.Services;

public class IssueCountGenerator
{
    private readonly string GitHubOrganization;
    private readonly string GitHubRepository;
    private readonly IGitHubClient client;
    private readonly Task<IssueClassificationModel> issueModelTask;

    public IssueCountGenerator(string gitHubOrganization, string gitHubRepository, IGitHubClient client, string configFolder = "")
    {
        GitHubOrganization = gitHubOrganization ?? throw new ArgumentNullException(nameof(gitHubOrganization));
        GitHubRepository = gitHubRepository ?? throw new ArgumentNullException(nameof(gitHubRepository));
        this.client = client ?? throw new ArgumentNullException(nameof(client));

        issueModelTask = IssueClassificationModel.CreateFromConfig(configFolder, gitHubOrganization.ToLower(), gitHubRepository.ToLower());
    }

    public async Task<DailyRecord> BuildIssuesForDate(DateOnly date)
    {
        var model = await issueModelTask;

        var productIssueCounts = new List<ProductIssueCount>();
        foreach (var prod in model.Products.
            Append(new Product { Label = "*", DisplayLabel = "Unnassigned", Technologies = Array.Empty<Technology>() }).
            Append(new Product { Label = null!, DisplayLabel = "Total", Technologies = Array.Empty<Technology>() }))
        {
            var technologyIssueCounts = new List<TechnologyIssueCount>();
            foreach (var tech in prod.Technologies
                .Append(new Technology { Label = null!, DisplayLabel = "Total" }))
            {
                var priorityIssueCounts = new List<PriorityIssueCount>();
                foreach (var priority in model.Priorities
                    .Append(new Priority { Label = "*", DisplayLabel = "Unassigned" })
                    .Append(new Priority { Label = null!, DisplayLabel = "Total" }))
                {
                    var classificationIssueCounts = new List<ClassificationIssueCount>();
                    foreach (var classification in model.Classification
                    .Append(new Classification { Label = "*", DisplayLabel = "Unassigned" })
                    .Append(new Classification { Label = null!, DisplayLabel = "Total" }))
                    {
                        string labelFilter = $"{model.Products.Filter(prod)} {prod.Technologies.Filter(tech)} {model.Priorities.Filter(priority)} {model.Classification.Filter(classification)}";
                        var query = new LabeledIssueCounts(client, GitHubOrganization, GitHubRepository, labelFilter);
                        var issueCount = await query.PerformQueryAsync();

                        classificationIssueCounts.Add(new ClassificationIssueCount(classification.Label, issueCount));
                    }
                    priorityIssueCounts.Add(new PriorityIssueCount(priority.Label, classificationIssueCounts.ToArray()));
                }
                technologyIssueCounts.Add(new TechnologyIssueCount(tech.Label, priorityIssueCounts.ToArray()));
            }
            productIssueCounts.Add(new ProductIssueCount(prod.Label, technologyIssueCounts.ToArray()));
        }

        var document = new DailyRecord
        {
            OrgAndRepo = $"{GitHubOrganization}/{GitHubRepository}",
            Date = date,
            Issues = productIssueCounts.ToArray()
        };

        return document;
    }
}

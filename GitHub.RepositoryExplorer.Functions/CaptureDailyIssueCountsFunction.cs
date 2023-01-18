using DotNetDocs.Tools.GitHubCommunications;
using GitHub.RepositoryExplorer.Functions.Configuration;
using GitHub.RepositoryExplorer.Models;
using GitHub.RepositoryExplorer.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.CosmosRepository;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace GitHub.RepositoryExplorer.Functions;

public class CaptureDailyIssueCountsFunction
{
    private readonly RepositoriesConfig _repositoriesConfig;
    private readonly IGitHubClient _gitHubClient;
    readonly IRepository<DailyRecord> _repository;

    public CaptureDailyIssueCountsFunction(IOptions<RepositoriesConfig> repositoriesConfig, IGitHubClient gitHubClient, IRepositoryFactory repositoryFactory)
    {
        _repositoriesConfig = repositoriesConfig.Value;
        _gitHubClient = gitHubClient;
        _repository = repositoryFactory.RepositoryOf<DailyRecord>();
    }

    [FunctionName("CaptureDailyIssueCountsFunction")]
    public async Task Run([TimerTrigger("0 0 10 * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var configFolder = Path.GetFullPath(Path.Combine(context.FunctionDirectory, ".."));
        foreach (var repositoryConfig in _repositoriesConfig.Repositories)
        {
            if (string.IsNullOrEmpty(repositoryConfig.RepositoryName) || string.IsNullOrEmpty(repositoryConfig.OrganizationName))
            {
                log.LogInformation("Invalid or missing GitHub repository configuration: {organization}/{repository}", repositoryConfig.OrganizationName, repositoryConfig.RepositoryName);
                continue;
            }

            log.LogInformation("Capturing daily ({date}) issue count for GitHub repository: {organization}/{repository}", date, repositoryConfig.OrganizationName, repositoryConfig.RepositoryName);

            var issueCountStorage = new IssueCountGenerator(repositoryConfig.OrganizationName, repositoryConfig.RepositoryName, _gitHubClient, configFolder);
            var dailyRecord = await issueCountStorage.BuildIssuesForDate(date);

            var query = new QueryDefinition(
                "SELECT c.id, c.orgAndRepo, c.date FROM c WHERE c.date = @date AND c.orgAndRepo = @orgAndRepo")
                .WithParameter("@date", date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .WithParameter("@orgAndRepo", $"{repositoryConfig.OrganizationName}/{repositoryConfig.RepositoryName}");

            var existingRecords = await _repository.GetByQueryAsync(query);
            if (existingRecords.Any())
            {
                var existingDailyRecord = existingRecords.First();
                dailyRecord.Id = existingDailyRecord.Id;
                await _repository.UpdateAsync(dailyRecord);
            }
            else
            {
                await _repository.CreateAsync(dailyRecord);
            }
        }
    }
}

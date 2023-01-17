using GitHub.RepositoryExplorer.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.CosmosRepository;

namespace GitHub.RepositoryExplorer.Services;
public class IssueCountStorage
{
    private readonly string _gitHubOrganization;
    private readonly string _gitHubRepository;
    private readonly IRepository<DailyRecord> _storage;

    public IssueCountStorage(
        IRepository<DailyRecord> storage,
        string gitHubOrganization,
        string gitHubRepository)
    {
        _gitHubOrganization = gitHubOrganization;
        _gitHubRepository = gitHubRepository;
        _storage = storage;
    }

    public async Task<DailyRecord> IssuesForDateAsync(DateOnly date)
    {
        var query = new QueryDefinition("SELECT * FROM c WHERE c.date = @date and c.orgAndRepo = @orgAndName")
                        .WithParameter("@date", date.ToString("o"))
                        .WithParameter("@orgAndName",$"{_gitHubOrganization}/{_gitHubRepository}");
        
        var existingRecords = await _storage.GetByQueryAsync(query);

        var dailyRecord = existingRecords.FirstOrDefault();
        return dailyRecord ?? throw new ArgumentException("Date out of range", nameof(date));
    }

    public ValueTask<IEnumerable<DailyRecord>> IssuesForDateRangeAsync(
        DateOnly from, DateOnly to)
    {
        var query = new QueryDefinition(
            "SELECT * FROM c WHERE (c.date BETWEEN @from AND @to) AND c.orgAndRepo = @orgAndName")
                .WithParameter("@from", from.ToString("o"))
                .WithParameter("@to", to.ToString("o"))
                .WithParameter("@orgAndName", $"{_gitHubOrganization}/{_gitHubRepository}");

        return _storage.GetByQueryAsync(query);
    }
}

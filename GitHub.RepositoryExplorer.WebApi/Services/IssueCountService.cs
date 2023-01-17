using Microsoft.Extensions.Caching.Memory;

namespace GitHub.RepositoryExplorer.Services;

public sealed class IssueCountService
{
    private readonly IRepository<DailyRecord> _storage;
    private readonly IMemoryCache _cache;

    public IssueCountService(
        IRepository<DailyRecord> storage, IMemoryCache cache) =>
        (_storage, _cache) = (storage, cache);

    public Task<DailyRecord?> GetForDateAsync(string org, string repo, DateOnly date)
    {
        var key = new DailyRecordKey(org, repo, date);
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            var service = new IssueCountStorage(_storage, org, repo);
            var dailyRecord = await service.IssuesForDateAsync(date);
            return dailyRecord;
        });
    }

    public Task<IEnumerable<DailyRecord>?> GetForRangeAsync(
        string org, string repo, DateOnly from, DateOnly to)
    {
        var key = new DailyRecordRangeKey(org, repo, from, to);
        return _cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
            var service = new IssueCountStorage(_storage, org, repo);
            var dailyRecords = await service.IssuesForDateRangeAsync(from, to);
            return dailyRecords;
        });
    }

    internal readonly record struct DailyRecordKey(
        string Org,
        string Repo,
        DateOnly Date);

   internal readonly record struct DailyRecordRangeKey(
        string Org,
        string Repo,
        DateOnly From,
        DateOnly To);
}
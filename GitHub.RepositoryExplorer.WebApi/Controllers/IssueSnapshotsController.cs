namespace GitHub.RepositoryExplorer.WebApi.Controllers;

[Route("api/snapshots"), EnableCors(PolicyName = CorsPolicy.Name)]
public class IssueSnapshotsController : ControllerBase
{
    private readonly IssueCountService _issueCountService;

    public IssueSnapshotsController(IssueCountService service) => _issueCountService = service;

    static DateOnly ParseDate(string date) => DateOnly.ParseExact(date, "o");

    /// <summary>
    /// Example API call: api/snapshots/dotnet/docs/2022-02-11
    /// </summary>
    [HttpPost, Route("{org}/{repo}/{date}")]
    public async Task<IEnumerable<IssuesSnapshot>?> GetForDate(
        [FromRoute] string org,
        [FromRoute] string repo,
        [FromRoute] string date,
        [FromBody] SnapshotKey[] allKeys)
    {
        var dailyRecord = await _issueCountService.GetForDateAsync(
                org, repo, ParseDate(date)) ?? throw new ArgumentException("No data for data", nameof(date));

        var rVal = new List<IssuesSnapshot>();
        foreach (var key in allKeys)
        {
            rVal.Add(dailyRecord.ToSnapshot(key));
        }
        return rVal;
    }

    /// <summary>
    /// Example API call: api/snapshots/dotnet/docs?from=2022-01-01&to=2022-01-31
    /// </summary>
    [
        HttpPost,
        Route("{org}/{repo}"),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<DailyRecord>), StatusCodes.Status200OK),
        ProducesResponseType(StatusCodes.Status500InternalServerError),
    ]
    public async Task<IEnumerable<IssuesSnapshot>> GetForRange(
        [FromRoute] string org,
        [FromRoute] string repo,
        [FromQuery] string from,
        [FromQuery] string to,
        [FromBody] SnapshotKey[] allKeys)
    {
        DateOnly fromDate = ParseDate(from);
        DateOnly toDate = ParseDate(to);
        var dailyRecords = await _issueCountService.GetForRangeAsync(
                org, repo, fromDate, ParseDate(to)) ?? throw new InvalidOperationException("No data returned");

        var rVal = new List<IssuesSnapshot>();
        foreach (var key in allKeys)
        {
            rVal.Add(dailyRecords.ToSnapshot(key, fromDate, toDate));
        }
        return rVal;
    }
}

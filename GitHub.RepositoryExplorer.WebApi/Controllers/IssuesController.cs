namespace GitHub.RepositoryExplorer.WebApi.Controllers;

[Route("api/issues"), EnableCors(PolicyName = CorsPolicy.Name)]
public class IssuesController : ControllerBase
{
    private readonly ILogger<IssuesController> _logger;
    private readonly IssueCountService _issueCountService;

    public IssuesController(
        ILogger<IssuesController> logger,
        IssueCountService service)
    {
        _logger = logger;
        _issueCountService = service;
    }

    static DateOnly ParseDate(string date) => DateOnly.ParseExact(date, "o");

    [HttpGet, Route("sanity")]
    public IActionResult DoesThisWork() => Ok();

    /// <summary>
    /// Example API call: api/issues/dotnet/docs/2022-02-11
    /// </summary>
    [HttpGet, Route("{org}/{repo}/{date}")]
    public async Task<DailyRecord?> GetForDate(
        [FromRoute] string org,
        [FromRoute] string repo,
        [FromRoute] string date)
    {
        try
        {
            return await _issueCountService.GetForDateAsync(
                org, repo, ParseDate(date));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForDate exception");
            return default;
        }
    }

    /// <summary>
    /// Example API call: api/issues/dotnet/docs?from=2022-01-01&to=2022-01-31
    /// </summary>
    [
        HttpGet,
        Route("{org}/{repo}"),
        Produces(MediaTypeNames.Application.Json),
        ProducesResponseType(typeof(IEnumerable<DailyRecord>), StatusCodes.Status200OK),
        ProducesResponseType(StatusCodes.Status500InternalServerError),
    ]
    public async Task<ActionResult<IEnumerable<DailyRecord>>> GetForRange(
        [FromRoute] string org,
        [FromRoute] string repo,
        [FromQuery] string from,
        [FromQuery] string to)
    {
        try
        {
            return new JsonResult(await _issueCountService.GetForRangeAsync(
                org, repo, ParseDate(from), ParseDate(to)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForRange exception");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

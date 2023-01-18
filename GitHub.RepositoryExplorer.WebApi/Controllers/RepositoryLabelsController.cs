using System.Reflection;

namespace GitHub.RepositoryExplorer.WebApi.Controllers;

[Route("api/repositorylabels"), EnableCors(PolicyName = CorsPolicy.Name)]
public class RepositoryLabelsController : ControllerBase
{
    private readonly ILogger<RepositoryLabelsController> _logger;

    public RepositoryLabelsController(
        ILogger<RepositoryLabelsController> logger) => _logger = logger;


    /// <summary>
    /// Example API call: api/repositorylabels/dotnet/docs
    /// </summary>
    [HttpGet, Route("{org}/{repo}"), ResponseCache(Duration = 28_800 /* 8 hours in seconds */)]
    public async Task<ActionResult<IssueClassificationModel>> GetLabels(
        [FromRoute] string org,
        [FromRoute] string repo)
    {
        try
        {
            var folder = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            var result = await IssueClassificationModel.CreateFromConfig(
                folder, org, repo);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetForDate exception");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}

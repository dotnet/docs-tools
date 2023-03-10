using System.Net.Http.Json;
using System.Web;

namespace GitHub.RepositoryExplorer.Client.Services;

public sealed class IssuesClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<DateOnly, string> _encode =
        static string (DateOnly date) => HttpUtility.UrlEncode($"{date:o}");

    public IssuesClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(HttpClientNames.IssuesApi);
    }

    public async Task<DailyRecord?> GetIssuesForDateAsync(
        Repository state, DateOnly date, IssueClassificationModel model)
    {
        var (org, repo) = (state.Org, state.Repo);
        var route =  _encode(date);
        try
        {
            var dailyRecord =
                await _httpClient.GetFromJsonAsync<DailyRecord>(
                    $"api/issues/{org}/{repo}/{route}");

            return dailyRecord;
        }
        catch ( Exception ex )
        {
            Console.WriteLine(ex);
            return DailyRecordFactory.CreateMissingRecord(date, model, org, repo);
        }
    }
}

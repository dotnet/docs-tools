using System.Text;
using System.Text.Json;
using System.Web;

namespace GitHub.RepositoryExplorer.Client.Services;

public sealed class IssuesByClassificationClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<DateOnly, string> _encode =
        static string (DateOnly date) => HttpUtility.UrlEncode($"{date:o}");

    public IssuesByClassificationClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(HttpClientNames.IssuesApi);
    }

    public async Task<IEnumerable<IssuesSnapshot>?> GetIssuesForDateRangeAsync(
        Repository state, DateOnly from, DateOnly to, RepoLabels labels)
    {
        var (org, repo) = (state.Org, state.Repo);
        var queryString =
            $"from={_encode(from)}&to={_encode(to)}";

        // TODO: This is going to be different for different graph sets
        var allKeys = new List<SnapshotKey>();
        foreach (var classificationKey in labels.IssueClassification.ClassificationWithUnassigned())
        {
            allKeys.Add(new SnapshotKey(Product: null,
                Technology: null,
                Priority: null,
                Classification: classificationKey.Label));
        }
        // end hack
        var content = new StringContent(JsonSerializer.Serialize(allKeys), Encoding.UTF8, "application/json");
        var response =
            await _httpClient.PostAsync(
                $"api/snapshots/{org}/{repo}?{queryString}", content);
        response.EnsureSuccessStatusCode();
        var jsonSnapshots = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<IEnumerable<IssuesSnapshot>>(jsonSnapshots, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}

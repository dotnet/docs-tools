namespace GitHub.RepositoryExplorer.Client.Services;

public class RepositoryLabelsClient
{
    private readonly HttpClient _httpClient;

    public RepositoryLabelsClient(IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient(HttpClientNames.IssuesApi);
    }

    public async Task<IssueClassificationModel?> GetRepositoryLabelsAsync(Repository state)
    {
        var (org, repo) = (state.Org, state.Repo);
        var result = 
            await _httpClient.GetFromJsonAsync<IssueClassificationModel>(
                $"api/repositorylabels/{org}/{repo}");

        return result;
    }
}

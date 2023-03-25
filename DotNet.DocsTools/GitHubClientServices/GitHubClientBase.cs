using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Polly;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace DotNetDocs.Tools.GitHubCommunications;

public abstract class GitHubClientBase : IGitHubClient, IDisposable
{
    private const string ProductID = "DotNetDocs.Tools";
    private const string ProductVersion = "2.0";
    private const string RESTendpoint = "https://api.github.com/repos";
    private static readonly Uri graphQLUri = new("https://api.github.com/graphql");
    private readonly HttpClient _client = new HttpClient();
    private readonly AsyncRetryPolicy _retryPolicy;
   
    internal GitHubClientBase()
    {
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductID, ProductVersion));
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(15), retryCount: 5);
        _retryPolicy = Policy
            .Handle<HttpRequestException>(ex =>
            {
                Console.WriteLine($"::warning::{ex}");
                return true;
            })
            .WaitAndRetryAsync(delay);
    }

    protected void SetAuthorizationHeader(AuthenticationHeaderValue Header) =>
        _client.DefaultRequestHeaders.Authorization = Header;

    protected async Task<int> RetrieveInstallationIDAsync()
    {
        using var response = await _client.GetAsync("https://api.github.com/app/installations");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"REST request failed for app installations");
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var installationId = jsonDocument.RootElement[0].GetProperty("id").GetInt32();
        return installationId;
    }

    protected async Task<(string oauthTokentoken, TimeSpan timeout)> RetrieveAuthTokenAsync(int installationID)
    {
        var tokenUrl = $"https://api.github.com/app/installations/{installationID}/access_tokens";
        using var emptyPacket = new StringContent("");
        using var tokenResponse = await _client.PostAsync(tokenUrl, emptyPacket);
        if (!tokenResponse.IsSuccessStatusCode)
            throw new InvalidOperationException($"REST request failed for app installations");
        var jsonDocument = await JsonDocument.ParseAsync(await tokenResponse.Content.ReadAsStreamAsync());

        var oauthToken = jsonDocument.RootElement.GetProperty("token").GetString() ??
            throw new ArgumentNullException("oauth token not found");
        var expiration = jsonDocument.RootElement.GetProperty("expires_at").GetString();
        var duration = (expiration is not null) ? DateTime.Parse(expiration) - DateTime.Now : TimeSpan.FromMinutes(50);
        return (oauthToken, duration);
    }

    async Task<JsonElement> IGitHubClient.PostGraphQLRequestAsync(GraphQLPacket queryText)
    {
        using var request = new StringContent(queryText.ToJsonText());
        request.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);
        var result = await _retryPolicy.ExecuteAndCaptureAsync(
            () => _client.PostAsync(graphQLUri, request));

        using var resp = result.Result;
        var jsonDocument = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
        var root = jsonDocument.RootElement;
        if (root.TryGetProperty("errors", out var errorList))
            throw new InvalidOperationException(errorList.GetRawText());
        else
            return root.GetProperty("data");
    }

    async Task<JsonDocument> IGitHubClient.GetReposRESTRequestAsync(params string[] restPath)
    {
        var url = RESTendpoint;
        foreach (var component in restPath)
            url += "/" + component;
        // Default single page result is 30, specify max items per page (100)
        url += "?per_page=100";
        var result = await _retryPolicy.ExecuteAndCaptureAsync(
            () => _client.GetAsync(url));
        using var response = result.Result;

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"REST request failed for {url}");
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return jsonDocument;
    }

    public void Dispose() => _client?.Dispose();
}

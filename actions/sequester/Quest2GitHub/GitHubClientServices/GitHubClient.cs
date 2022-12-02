using Polly.Contrib.WaitAndRetry;
using Polly;
using Polly.Retry;
using System;

namespace DotnetDocsTools.GitHubCommunications;

public sealed class GitHubClient : IGitHubClient, IDisposable
{
    private const string ProductID = "DotnetDocsTools";
    private const string ProductVersion = "2.0";

    private static readonly Uri markdownUri = new("https://api.github.com/markdown");
    private static readonly Uri graphQLUri = new("https://api.github.com/graphql");
    private const string RESTendpoint = "https://api.github.com/repos";
    private readonly HttpClient _client;
    private readonly AsyncRetryPolicy _retryPolicy;

    public GitHubClient(string token)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductID, ProductVersion));
        var delay = Backoff.DecorrelatedJitterBackoffV2(
            medianFirstRetryDelay: TimeSpan.FromSeconds(15), retryCount: 5);
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(delay);
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

    async Task<string> IGitHubClient.PostMarkdownRESTRequestAsync(string markdownText)
    {
        var requestBody = new MarkdownToHtmlRequest
        {
            text = markdownText
        };
        using var request = new StringContent(requestBody.ToJsonText());
        request.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        request.Headers.Add("Accepts", MediaTypeNames.Text.Html);
        var result = await _retryPolicy.ExecuteAndCaptureAsync(
            () => _client.PostAsync(markdownUri, request));

        using var resp = result.Result;
        var stringResponse = await resp.Content.ReadAsStringAsync();
        return stringResponse;
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

    async IAsyncEnumerable<string> IGitHubClient.GetContentAsync(string link)
    {
        var result = await _retryPolicy.ExecuteAndCaptureAsync(
            () => _client.GetAsync(link));

        using var response = result.Result;
        
        if (!response.IsSuccessStatusCode)
            yield return "";
        else
        {
            using var responseStream = await response.Content.ReadAsStreamAsync();
            using StreamReader reader = new(responseStream);
            var line = await reader.ReadLineAsync();
            while (line != null)
            {
                yield return line;
                line = await reader.ReadLineAsync();
            }
        }
    }

    public void Dispose() => _client?.Dispose();
}
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;

namespace DotnetDocsTools.GitHubCommunications;

internal class GitHubClient : IGitHubClient
{
    private const string ProductID = "DotnetDocsTools";
    private const string ProductVersion = "2.0";

    private static readonly Uri markdownUri = new Uri("https://api.github.com/markdown");
    private static readonly Uri graphQLUri = new Uri("https://api.github.com/graphql");
    private const string RESTendpoint = "https://api.github.com/repos";
    private readonly HttpClient client;

    internal GitHubClient(string token)
    {
        client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(ProductID, ProductVersion));
    }

    async Task<JsonElement> IGitHubClient.PostGraphQLRequestAsync(GraphQLPacket queryText)
    {
        using var request = new StringContent(queryText.ToJsonText());
        request.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
        request.Headers.Add("Accepts", MediaTypeNames.Application.Json);
        using var resp = await client.PostAsync(graphQLUri, request);

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
        using var resp = await client.PostAsync(markdownUri, request);
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

        using var resp = await client.GetAsync(url);
        using HttpResponseMessage response = await client.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"REST request failed for {url}");
        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return jsonDocument;
    }

    async IAsyncEnumerable<string> IGitHubClient.GetContentAsync(string link)
    {
        HttpResponseMessage response = await client.GetAsync(link);
        if (!response.IsSuccessStatusCode)
            yield return "";
        else
        {
            var responseStream = await response.Content.ReadAsStreamAsync();
            StreamReader reader = new StreamReader(responseStream);
            var line = await reader.ReadLineAsync();
            while (line != null)
            {
                yield return line;
                line = await reader.ReadLineAsync();
            }
        }
    }
}
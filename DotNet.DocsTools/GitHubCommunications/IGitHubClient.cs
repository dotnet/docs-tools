using System.Text.Json;

namespace DotnetDocsTools.GitHubCommunications;

/// <summary>
/// Interface that defines all communication between with the GitHub web APIs.
/// </summary>
/// <remarks>
/// The class that implements this interface is intended to 
/// be the only type and object that communicates with the
/// GitHub endpoint. The APIs send and receive requests
/// to the GitHub endpoints (GraphQL, REST, or Markdown2HTML).
/// </remarks>
public interface IGitHubClient : IDisposable
{
    /// <summary>
    /// Post a request to the GraphQL endpoint.
    /// </summary>
    /// <param name="queryText">The post packet for the query.</param>
    /// <returns>The string response.</returns>
    /// <remarks>
    /// Post the request, then parse the response and return
    /// the root data element of the request response.
    /// </remarks>
    Task<JsonElement> PostGraphQLRequestAsync(GraphQLPacket queryText);

    /// <summary>
    /// Send markdown to convert to HTML
    /// </summary>
    /// <param name="markdownText">The text to convert to HTML</param>
    /// <returns>The HTML content, as a string</returns>
    /// <remarks>
    /// This method posts a request to the GitHub markdown
    /// endpoint, requesting to convert from markdown to HTML.
    /// If the request succeeds, the respons is the HTML text.
    /// </remarks>
    Task<string> PostMarkdownRESTRequestAsync(string markdownText);

    /// <summary>
    /// Execute a GET request to the REST repository endpoint.
    /// </summary>
    /// <param name="restPath">The remaining folder paths of the request.</param>
    /// <returns>The JSONDocument for the response.</returns>
    /// <remarks>
    /// Send the GET request to the API endpoint, then
    /// process the response for a success code and return the parsed
    /// JSONdocument.
    /// </remarks>
    Task<JsonDocument> GetReposRESTRequestAsync(params string[] restPath);

    /// <summary>
    /// Retrieve the content from a (usually raw) URL.
    /// </summary>
    /// <param name="link">The URL to retrieve.</param>
    /// <returns>An async enumerable for each line in the content at the URL.</returns>
    /// <remarks>
    /// This isn't GitHub API specific, but was moved into this class to reuse
    /// the same HttpClient instance.
    /// </remarks>
    IAsyncEnumerable<string> GetContentAsync(string link);

    /// <summary>
    /// Create a default GitHub client.
    /// </summary>
    /// <param name="token">The API token.</param>
    /// <returns>The new GitHubClient object.</returns>
    /// <remarks>
    /// An application should create only one client, either
    /// by using a DI container, or by creating a singleton
    /// for the single GitHub client.
    /// </remarks>
    public static IGitHubClient CreateGitHubClient(string token) => 
        new GitHubClient(token);
}

using DotNet.DocsTools.GitHubClientServices;
using System.Text.Json;

namespace DotNetDocs.Tools.GitHubCommunications;

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

    /// <summary>
    /// Create a GitHub app authorized GitHub client.
    /// </summary>
    /// <param name="appID">The GitHub app ID</param>
    /// <param name="oauthPrivateKey">The private key for the GitHub app oauth</param>
    /// <returns>The GitHub client.</returns>
    public static async Task<IGitHubClient> CreateGitHubAppClient(int appID, string oauthPrivateKey)
    {
        Console.WriteLine("Using AppID, Private Key tokens.");
        var client = new GitHubAppClient(appID, oauthPrivateKey);
        await client.GenerateTokenAsync();

        return client;
    }
}

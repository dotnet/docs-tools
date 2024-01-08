using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// This runs a query that returns an enumeration of GitHub objects.
/// </summary>
/// <typeparam name="TResult">The type of the result objects.</typeparam>
/// <typeparam name="TVariables">A record type containing the variables of the query. </typeparam>
/// <remarks>
/// This generic type performs a query of GitHub's GraphQL endpoint, and returns an async
/// enumerable of the result objects. The query is parameterized by a record type, and the
/// result type. The result type must implement the <see cref="IGitHubQueryResult{TResult, TVariables}"/>
/// </remarks>
public class ScalarQuery<TResult, TVariables> where TResult : IGitHubQueryResult<TResult, TVariables>
{
    private readonly IGitHubClient _client;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    public ScalarQuery(IGitHubClient client)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        _client = client;
    }

    /// <summary>
    /// Run the async query.
    /// </summary>
    /// <returns>The async enumerable.</returns>
    /// <remarks>
    /// This query encapsulates the paging API for GitHub's GraphQL 
    /// endpoint.
    /// </remarks>
    public async Task<TResult?> PerformQuery(TVariables variables)
    {
        var scalarPacket = TResult.GetQueryPacket(variables, true);

        var rootElement= await _client.PostGraphQLRequestAsync(scalarPacket);

        // TODO: This navigation should likely move to the FromJsonElement.
        var issueNode = rootElement.Descendent(TResult.NavigationToNodes(true));
        return TResult.FromJsonElement(issueNode, variables);
    }
}

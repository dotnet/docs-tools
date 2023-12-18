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
public class EnumerationQuery<TResult, TVariables> where TResult : IGitHubQueryResult<TResult, TVariables>
{
    private readonly IGitHubClient _client;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    public EnumerationQuery(IGitHubClient client)
    {
        _client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
    }

    /// <summary>
    /// Run the async query.
    /// </summary>
    /// <returns>The async enumerable.</returns>
    /// <remarks>
    /// This query encapsulates the paging API for GitHub's GraphQL 
    /// endpoint.
    /// </remarks>
    public async IAsyncEnumerable<TResult> PerformQuery(TVariables variables)
    {
        var findIssuesPacket = TResult.GetQueryPacket(variables);

        var cursor = default(string);
        bool hasMore = true;
        while (hasMore)
        {
            findIssuesPacket.variables["cursor"] = cursor!;
            var jsonData = await _client.PostGraphQLRequestAsync(findIssuesPacket);

            (hasMore, cursor) = jsonData.Descendent(TResult.NavigationToNodes(false)).NextPageInfo();

            var elements = jsonData.Descendent(TResult.NavigationToNodes(false).Append("nodes")).EnumerateArray();
            foreach (var item in elements)
                yield return TResult.FromJsonElement(item, variables);
        }
    }
}

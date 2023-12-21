using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GraphQLQueries;

/// <summary>
/// Generic mutation class for GitHub GraphQL mutations.
/// </summary>
/// <typeparam name="TMutationQuery">The specific mutation</typeparam>
/// <typeparam name="TVariables">The set of variables.</typeparam>
public class Mutation<TMutationQuery, TVariables> where TMutationQuery : IGitHubMutation<TMutationQuery, TVariables>
{
    private readonly IGitHubClient _client;

    /// <summary>
    /// Construction a mutation.
    /// </summary>
    /// <param name="client">The GitHub client object.</param>
    public Mutation(IGitHubClient client)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        _client = client;
    }

    /// <summary>
    /// Perform the mutation.
    /// </summary>
    /// <param name="mutationVariables">The variables type</param>
    /// <returns>The task that contains the result</returns>
    /// <remarks>
    /// This method constructs the packet for the GraphQL mutation,
    /// and then posts the mutation to GitHub.
    /// </remarks>
    public async Task PerformMutation(TVariables mutationVariables)
    {
        var mutationPacket = TMutationQuery.GetMutationPacket(mutationVariables);
        _ = await _client.PostGraphQLRequestAsync(mutationPacket);
    }
}

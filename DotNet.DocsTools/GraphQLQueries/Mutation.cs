using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GraphQLQueries;

public class Mutation<TMutationQuery, TVariables> where TMutationQuery : IGitHubMutation<TMutationQuery, TVariables>
{
    private readonly IGitHubClient _client;

    public Mutation(IGitHubClient client)
    {
        ArgumentNullException.ThrowIfNull(client, nameof(client));
        _client = client;
    }

    public async Task PerformMutation(TVariables mutationVariables)
    {
        var mutationPacket = TMutationQuery.GetMutationPacket(mutationVariables);
        _ = await _client.PostGraphQLRequestAsync(mutationPacket);
    }
}

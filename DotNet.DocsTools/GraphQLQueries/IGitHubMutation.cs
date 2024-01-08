using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GraphQLQueries;


/// <summary>
/// Interface that collaborates with the <see cref="Mutation{TMutationQuery, TVariables}"/> class to
/// perform a GitHub mutation.
/// </summary>
/// <typeparam name="TMutation">The class that performs a specific mutation.</typeparam>
/// <typeparam name="TVariables">A record that contains the types for the variables in the GraphQL packet.</typeparam>
public interface IGitHubMutation<TMutation, TVariables> where TMutation : IGitHubMutation<TMutation, TVariables>
{
    /// <summary>
    /// Return the GraphQL packet for the mutation.
    /// </summary>
    /// <param name="variables">A record that defines the variables.</param>
    /// <returns>The query packet.</returns>
    public abstract static GraphQLPacket GetMutationPacket(TVariables variables);
}

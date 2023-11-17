using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// This interface ties a GitHub object type to a query text, and its variables.
/// </summary>
/// <remarks>
/// The "shape" of most queries is the same. Create a query, execute it, and transform
/// the results into Json. The static abstract methods in this interface tie the query
/// text to the GitHub object type.</remarks>
/// <typeparam name="TResult">The result type of objects returned by the query</typeparam>
/// <typeparam name="TVariables">A record that has members for each of the variables in the query.</typeparam>
public interface IGitHubQueryResult<TResult, TVariables> where TResult : IGitHubQueryResult<TResult, TVariables>
{
    /// <summary>
    /// Construct the <typeparamref name="TResult"/> object from the JsonElement.
    /// </summary>
    /// <param name="element">The Json element representing one object.</param>
    /// <returns>An instance of the result type.</returns>
    public abstract static TResult FromJsonElement(JsonElement element);

    /// <summary>
    /// Construct the query packet for this query.
    /// </summary>
    /// <param name="variables">
    /// An instance of a record whose members are transformed into the dictionary for the query.
    /// </param>
    /// <returns>The Packet structure for the query.</returns>
    public abstract static GraphQLPacket GetQueryPacket(TVariables variables);
}

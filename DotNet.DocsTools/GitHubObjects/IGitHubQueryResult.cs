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
    /// Construct the query packet for this query.
    /// </summary>
    /// <param name="variables">
    /// An instance of a record whose members are transformed into the dictionary for the query.
    /// </param>
    /// <param name="isScalar">True to return the query for a scalar query. False for an enumeration query.</param>
    /// <returns>The Packet structure for the query.</returns>
    public static abstract GraphQLPacket GetQueryPacket(TVariables variables, bool isScalar);

    /// <summary>
    /// Construct the <typeparamref name="TResult"/> object from the JsonElement.
    /// </summary>
    /// <param name="element">The Json element representing one object.</param>
    /// <returns>An instance of the result type.</returns>
    public static abstract TResult? FromJsonElement(JsonElement element, TVariables variables);

    /// <summary>
    /// Retrieve the path to the correct data node from the "data" JsonElement node.
    /// </summary>
    /// <param name="isScalar">True if the query is a scalar query. False if it's enumerating an array</param>
    /// <returns>
    /// The sequence of node names to traverse from the "data" node to the node representing
    /// the object being returned.
    /// </returns>
    /// <remarks>
    /// For scalar queries, this navigation should return the node containing the result.
    /// For array queries, this navigation should return the parent of the "nodes" element,
    /// where the "nodes" element contains the array being enumerated.
    /// </remarks>
    public static abstract IEnumerable<string> NavigationToNodes(bool isScalar);
}

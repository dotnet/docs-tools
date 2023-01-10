using System.Text.Json;

namespace DotnetDocsTools.GitHubCommunications;

/// <summary>
/// This basic class represents a GraphQL post packet.
/// </summary>
/// <remarks>
/// This class represents a query packet. It has the shape expected
/// for a GraphQL query.
/// </remarks>
public class GraphQLPacket
{
    /// <summary>
    /// The query or mutation string.
    /// </summary>
    public string query { get; set; } = "";

    /// <summary>
    /// The dictionary of variables for the query.
    /// </summary>
    public IDictionary<string, object> variables { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Convert this object to JSON.
    /// </summary>
    /// <returns>The minified JSON for this object.</returns>
    public string ToJsonText() => JsonSerializer.Serialize(this);
}

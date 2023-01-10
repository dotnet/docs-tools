using DotnetDocsTools.GitHubCommunications;

namespace DotnetDocsTools.GraphQLQueries;

public class CloseIssueMutation
{
    private static readonly string closeIssueMutationText =
@"mutation CloseIssue($nodeID: ID!) { 
  closeIssue(input: {
    issueId:$nodeID,
    clientMutationId: ""dotnet-docs-tools""
  }) {
    clientMutationId
 }
}";

    private readonly IGitHubClient client;
    private readonly string nodeId;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="nodeId">The node to modify</param>
    public CloseIssueMutation(IGitHubClient client, string nodeId)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.nodeId = !string.IsNullOrWhiteSpace(nodeId)
            ? nodeId
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(nodeId));
    }

    /// <summary>
    /// Perform the mutation.
    /// </summary>
    /// <returns>A task to be awaited.</returns>
    /// <remarks>
    /// I haven't see failures from the GitHub endpoint reflected
    /// in the result packet. However, on rare occasions, the 
    /// mutation fails. This should be updated once I see why it fails.
    /// </remarks>
    public async Task PerformMutation()
    {
        var commentPacket = new GraphQLPacket
        {
            query = closeIssueMutationText,
            variables =
            {
                ["nodeID"] = nodeId,
            }
        };
        var jsonData = await client.PostGraphQLRequestAsync(commentPacket);
        // TODO: check for errors
    }
}

using DotNetDocs.Tools.GitHubCommunications;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// GraphQL Object to add a comment to an existing issue.
/// </summary>
/// <remarks>
/// To use this, create the mutation and set the node and comment body. 
/// Then, perform the mutation.
/// </remarks>
public class AddCommentMutation
{
    private static readonly string addCommentMutationText = """
    mutation AddComment($nodeID: ID!, $commentText: String!) { 
      addComment(input: {
        subjectId:$nodeID,
        body:$commentText,
        clientMutationId: "dotnet-docs-tools"
      }) {
        clientMutationId
      }
    }
    """;
    private readonly IGitHubClient client;
    private readonly string nodeId;
    private readonly string commentBody;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="nodeId">The node to modify</param>
    /// <param name="commentBody">The body of the comment</param>
    public AddCommentMutation(IGitHubClient client, string nodeId, string commentBody)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.nodeId = !string.IsNullOrWhiteSpace(nodeId)
            ? nodeId
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(nodeId));
        this.commentBody = !string.IsNullOrWhiteSpace(commentBody)
            ? commentBody
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(commentBody));
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
            query = addCommentMutationText,
            variables =
            {
                ["nodeID"] = nodeId,
                ["commentText"] = commentBody
            }
        };
        var jsonData = await client.PostGraphQLRequestAsync(commentPacket);
        // TODO: check for errors
    }
}

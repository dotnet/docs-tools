namespace DotnetDocsTools.GraphQLQueries;

/// <summary>
/// Add or remove a label.
/// </summary>
/// <remarks>
/// This is misnamed. This is where I was working when I found that
/// one mutation operation can contain multiple requests.
/// In time, I'll refactor all the code for mutations to use more
/// of a builder algorithm that constructs different mutation packets,
/// adds variables for the overall operation, and then runs the mutation.
/// </remarks>
public class AddOrRemoveLabelMutation
{
    private static readonly string removeLabelMutationText = """
    mutation RemoveLabels($nodeID: ID!, $labelIDs: [ID!]!) {
      removeLabelsFromLabelable(input: {
        labelableId:$nodeID
        labelIds:$labelIDs
        clientMutationId:"dotnet-docs-tools"
      }) {
        labelable {
          __typename
        }
        clientMutationId
      }
    }
    """;

    private static readonly string addLabelMutationText = """
    mutation AddLabels($nodeID: ID!, $labelIDs: [ID!]!) {
        addLabelsToLabelable(input: {
          labelableId:$nodeID
          labelIds:$labelIDs
          clientMutationId:"dotnet-docs-tools"
        }) {
          labelable {
            __typename
        }
        clientMutationId
        }
    }
    """;

    private static readonly string addremoveLabelMutationText = """
        mutation AddRemoveLabels($bodyText: String!, $nodeID: ID!, $addedLabelIDs: [ID!]!, $deletedLabelIDs: [ID!]!) {
          removeLabelsFromLabelable(
            input: {labelableId: $nodeID, labelIds: $deletedLabelIDs, clientMutationId: "dotnet-docs-tools"}
          ) {
            labelable {
              __typename
            }
            clientMutationId
          }
          updateIssue (
            input: {body: $bodyText, clientMutationId:"dotnet-docs-tools", id:$nodeID }
          ) {
            clientMutationId
          }
        addLabelsToLabelable(
            input: {labelableId: $nodeID, labelIds: $addedLabelIDs, clientMutationId: "dotnet-docs-tools"}
          ) {
            labelable {
              __typename
            }
            clientMutationId
          }
        }
        """;

    private readonly IGitHubClient client;
    private readonly string nodeId;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="nodeId">The node to modify</param>
    public AddOrRemoveLabelMutation(IGitHubClient client, string nodeId)
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
    public async Task PerformMutation(string updatedBody, string? labelToAdd, string? labelToRemove)
    {
        var labelPacket = (labelToAdd, labelToRemove) switch
        {
            (null, null) => throw new ArgumentException("Must specify a label to add or remove"),
            (_, null) => new GraphQLPacket
                {
                    query = addLabelMutationText,
                    variables =
                    {
                        ["nodeID"] = nodeId,
                        ["labelIDs"] = labelToAdd
                    }
                },
            (null, _) => new GraphQLPacket
                {
                    query = removeLabelMutationText,
                    variables =
                        {
                            ["nodeID"] = nodeId,
                            ["labelIDs"] = labelToRemove
                        }
                },
            (_, _) => new GraphQLPacket
                {
                    query = addremoveLabelMutationText,
                    variables =
                        {
                            ["nodeID"] = nodeId,
                            ["bodyText"] = updatedBody,
                            ["addedLabelIDs"] = labelToAdd,
                            ["deletedLabelIDs"] = labelToRemove,

                        }
                },
        };
            
        var jsonData = await client.PostGraphQLRequestAsync(labelPacket);
        // TODO: check for errors
    }
}

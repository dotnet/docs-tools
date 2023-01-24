using DotNetDocs.Tools.GitHubCommunications;
using System;
using System.Threading.Tasks;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// Add or remove a label.
/// </summary>
/// <remarks>
/// This class performs a mutation to remove or
/// add a label to a "labelable" node.
/// </remarks>
public class AddOrRemoveLabelMutation
{
    private static readonly string removeLabelMutationText =
@"mutation RemoveLabels($nodeID: ID!, $labelIDs: [ID!]!) {
  removeLabelsFromLabelable(input: {
    labelableId:$nodeID
    labelIds:$labelIDs
    clientMutationId:""dotnet-docs-tools""
  }) {
    labelable {
      __typename
    }
    clientMutationId
  }
}
";

    private static readonly string addLabelMutationText =
@"mutation AddLabels($nodeID: ID!, $labelIDs: [ID!]!) {
  addLabelsToLabelable(input: {
    labelableId:$nodeID
    labelIds:$labelIDs
    clientMutationId:""dotnet-docs-tools""
  }) {
    labelable {
      __typename
    }
    clientMutationId
  }
}
";
    private readonly IGitHubClient client;
    private readonly string nodeId;
    private readonly string labelId;
    private readonly bool addLabel;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <param name="nodeId">The node to modify</param>
    /// <param name="labelId">The id of the label to add or remove</param>
    /// <param name="add">True to add, false to remove.</param>
    public AddOrRemoveLabelMutation(IGitHubClient client, string nodeId, string labelId, bool add)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.nodeId = !string.IsNullOrWhiteSpace(nodeId)
            ? nodeId
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(nodeId));
        this.labelId = !string.IsNullOrWhiteSpace(labelId)
            ? labelId
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(labelId));
        this.addLabel = add;
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
        var labelPacket = new GraphQLPacket
        {
            query = addLabel ? addLabelMutationText : removeLabelMutationText,
            variables =
            {
                ["nodeID"] = nodeId,
                ["labelIDs"] = labelId
            }
        };
        var jsonData = await client.PostGraphQLRequestAsync(labelPacket);
        // TODO: check for errors
    }
}

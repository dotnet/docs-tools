using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The variables for the sequester mutation.
/// </summary>
/// <param name="NodeID">The id of the issue to update</param>
/// <param name="LabelIDToRemove">The ID of the label to remove</param>
/// <param name="LabelIDToAdd">The ID of the label to add</param>
/// <param name="BodyText">The updated body text</param>
public readonly record struct SequesterVariables(string NodeID, string LabelIDToRemove, string LabelIDToAdd, string BodyText);

/// <summary>
/// The mutation to sequester an issue.
/// </summary>
/// <remarks>
/// This performs three distinct actions in order. First, the "request" label is removed. Second,
/// the body text is updated to add a link to the AzDo work item. Third, the "sequestered label
/// is added.
/// </remarks>
public class SequesteredIssueMutation : IGitHubMutation<SequesteredIssueMutation, SequesterVariables>
{
    private const string sequesterPacketText = """
      mutation SequesterIssue($nodeID: ID!, $bodyText: String!, $addLabelIDs: [ID!]!, $removeLabelIDs: [ID!]!) {
        removeLabelsFromLabelable(input: {
          labelableId:$nodeID
          labelIds:$removeLabelIDs
          clientMutationId:"dotnet-docs-tools"
        }) {
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
        addLabelsToLabelable(input: {
          labelableId:$nodeID
          labelIds:$addLabelIDs
          clientMutationId:"dotnet-docs-tools"
        }) {
          labelable {
            __typename
          }
          clientMutationId
        }
      }
      """;

    /// <summary>
    /// Construct the mutation packet
    /// </summary>
    /// <param name="variables">The set of variables to use.</param>
    /// <returns>The packet, including all variables.</returns>
    public static GraphQLPacket GetMutationPacket(SequesterVariables variables) => 
        new()
        {
            query = sequesterPacketText,
            variables =
            {
                ["nodeID"] = variables.NodeID,
                ["bodyText"] = variables.BodyText,
                ["addLabelIDs"] = new[] { variables.LabelIDToAdd },
                ["removeLabelIDs"] = new[] { variables.LabelIDToRemove }
            }
        };
}

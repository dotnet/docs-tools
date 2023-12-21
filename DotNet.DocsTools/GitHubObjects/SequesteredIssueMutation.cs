using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

public readonly record struct SequesterVariables(string NodeID, string LabelIDToRemove, string LabelIDToAdd, string BodyText);
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

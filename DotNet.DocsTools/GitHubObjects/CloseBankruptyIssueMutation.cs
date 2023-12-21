using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

public readonly record struct CloseBankruptcyIssueVariables(string NodeID, string LabelID, string CommentText);

public class CloseBankruptyIssueMutation : IGitHubMutation<CloseBankruptyIssueMutation, CloseBankruptcyIssueVariables>
{
    private const string mutationPacketText = """
        mutation AddLabels($nodeID: ID!, $labelIDs: [ID!]!, $commentText: String!) {
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
          addComment(input: {
            subjectId:$nodeID,
            body:$commentText,
            clientMutationId: "dotnet-docs-tools"
          }) {
            clientMutationId
          }
          closeIssue(input: {
            issueId:$nodeID,
            clientMutationId: "dotnet-docs-tools"
          }) {
            clientMutationId
         }
        }
        """;

    public static GraphQLPacket GetMutationPacket(CloseBankruptcyIssueVariables variables) =>
        new()
        {
            query = mutationPacketText,
            variables = 
            {
                ["nodeID"] = variables.NodeID,
                ["labelIDs"] = new[] { variables.LabelID },
                ["commentText"] = variables.CommentText
            }
        };
}

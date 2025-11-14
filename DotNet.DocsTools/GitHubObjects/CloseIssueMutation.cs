using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// Variables for the CloseBankruptcyIssueMutation.
/// </summary>
/// <param name="NodeID">The node ID for the issue to close.</param>
/// <param name="LabelID">The label ID (typically for a "won't fix" label) to add before closing.</param>
/// <param name="CommentText">The comment text to add to the issue as a close message.</param>
public readonly record struct CloseIssueVariables(string NodeID, string? LabelID, string CommentText);

/// <summary>
/// Mutation to close an issue.
/// </summary>
public class CloseIssueMutation : IGitHubMutation<CloseIssueMutation, CloseIssueVariables>
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

    /// <summary>
    /// Construct the GraphQL packet for closing an issue.
    /// </summary>
    /// <param name="variables">The variables that determines which issue to close.</param>
    /// <returns>The GraphQL packet object.</returns>
    public static GraphQLPacket GetMutationPacket(CloseIssueVariables variables) =>
        new()
        {
            query = mutationPacketText,
            variables = 
            {
                ["nodeID"] = variables.NodeID,
                ["labelIDs"] = variables.LabelID is not null ? [variables.LabelID] : Array.Empty<string>(),
                ["commentText"] = variables.CommentText
            }
        };
}

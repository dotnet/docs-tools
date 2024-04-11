using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The mutation to sequester an issue.
/// </summary>
/// <remarks>
/// This performs three distinct actions in order. First, the "request" label is removed. Second,
/// the body text is updated to add a link to the AzDo work item. Third, the "sequestered label
/// is added.
/// </remarks>
public class SequesteredIssueMutation : SequesteredIssueOrPullRequestMutation, IGitHubMutation<SequesteredIssueMutation, SequesterVariables>
{
    /// <summary>
    /// Construct the mutation packet
    /// </summary>
    /// <param name="variables">The set of variables to use.</param>
    /// <returns>The packet, including all variables.</returns>
    public static GraphQLPacket GetMutationPacket(SequesterVariables variables) =>
        GetMutationPacket(variables, true);
}

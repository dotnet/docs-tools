using DotNet.DocsTools.GraphQLQueries;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;
public class SequesteredPullRequestMutation : SequesteredIssueOrPullRequestMutation, IGitHubMutation<SequesteredPullRequestMutation, SequesterVariables>
{
    /// <summary>
    /// Construct the mutation packet
    /// </summary>
    /// <param name="variables">The set of variables to use.</param>
    /// <returns>The packet, including all variables.</returns>
    public static GraphQLPacket GetMutationPacket(SequesterVariables variables) =>
        GetMutationPacket(variables, false);
}

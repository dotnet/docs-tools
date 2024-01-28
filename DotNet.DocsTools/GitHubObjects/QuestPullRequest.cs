using System.Text.Json;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;
public sealed record QuestPullRequest : QuestIssueOrPullRequest, IGitHubQueryResult<QuestPullRequest, QuestIssueOrPullRequestVariables>
{
    /// <summary>
    /// Construct the query packet for the given variables
    /// </summary>
    /// <param name="variables">The variables added to the packet</param>
    /// <returns>The GraphQL Packet structure.</returns>
    /// <exception cref="ArgumentException">Thrown when one of the required fields in the variables packet is null.</exception>
    public static GraphQLPacket GetQueryPacket(QuestIssueOrPullRequestVariables variables, bool isScalar) => isScalar
        ? new()
        {
            query =QuestIssueScalarQueryText.Replace("issue(number: $issueNumber)", "pullRequest(number: $issueNumber)"),
            variables =
                {
                    ["organization"] = variables.Organization,
                    ["repository"] = variables.Repository,
                    ["issueNumber"] = variables.issueNumber ?? throw new ArgumentException("The issue number can't be null"),
                }
        }
        : new GraphQLPacket
        {
            query = EnumerateQuestIssuesQueryText.Replace("issues(", "pullRequests("),
            variables =
                {
                    ["organization"] = variables.Organization,
                    ["repository"] = variables.Repository,
                    ["questlabels"] = new string[]
                    {
                        variables.importTriggerLabelText ?? throw new ArgumentException("The import trigger label can't be null"),
                        variables.importedLabelText ?? throw new ArgumentException("The imported label can't be null")
                    }
                }
        };

    public static IEnumerable<string> NavigationToNodes(bool isScalar) => ["repository", "pullRequests"];

    /// <summary>
    /// Construct a QuestIssue from a JsonElement
    /// </summary>
    /// <param name="issueNode">The JSON issue node</param>
    /// <param name="variables">The variables used in the query.</param>
    /// <returns></returns>
    public static QuestPullRequest FromJsonElement(JsonElement issueNode, QuestIssueOrPullRequestVariables variables) =>
        new QuestPullRequest (issueNode, variables.Organization, variables.Repository);

    private QuestPullRequest(JsonElement issueNode, string organization, string repository) : base(issueNode, organization, repository) { }
}

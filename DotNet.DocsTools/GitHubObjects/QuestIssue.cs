using System.Text.Json;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNet.DocsTools.GitHubObjects;
public record QuestIssue : QuestIssueOrPullRequest, IGitHubQueryResult<QuestIssue, QuestIssueOrPullRequestVariables>
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
            query = QuestIssueScalarQueryText,
            variables =
                {
                    ["organization"] = variables.Organization,
                    ["repository"] = variables.Repository,
                    ["issueNumber"] = variables.issueNumber ?? throw new ArgumentException("The issue number can't be null"),
                }
        }
        : new GraphQLPacket
        {
            query = EnumerateQuestIssuesQueryText,
            variables =
                {
                    ["organization"] = variables.Organization,
                    ["repository"] = variables.Repository,
                    ["states"] = variables.states.Any() ? variables.states : ["OPEN", "CLOSED"],
                    ["questlabels"] = new string[]
                    {
                        variables.importTriggerLabelText ?? throw new ArgumentException("The import trigger label can't be null"),
                        variables.importedLabelText ?? throw new ArgumentException("The imported label can't be null"),
                        variables.removeLabelText ?? throw new ArgumentException("The remove label can't be null"),
                        variables.localizationLabelText ?? throw new ArgumentException("The localization label can't be null")
                    }
                }
        };

    public static IEnumerable<string> NavigationToNodes(bool isScalar) =>
        (isScalar) ?["repository", "issue"] : ["repository", "issues"];

    /// <summary>
    /// Construct a QuestIssue from a JsonElement
    /// </summary>
    /// <param name="issueNode">The JSON issue node</param>
    /// <param name="variables">The variables used in the query.</param>
    /// <returns></returns>
    public static QuestIssue FromJsonElement(JsonElement issueNode, QuestIssueOrPullRequestVariables variables) =>
        new(issueNode, variables.Organization, variables.Repository);

    private QuestIssue(JsonElement issueNode, string organization, string repository) : base(issueNode, organization, repository)
    {
    }

}

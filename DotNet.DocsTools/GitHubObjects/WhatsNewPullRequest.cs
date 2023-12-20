using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using DotNetDocs.Tools.Utility;
using System.Buffers;
using System.Reflection.Emit;
using System.Text.Json;

namespace DotNetDocs.Tools.GitHubObjects;

/// <summary>
/// Variables needed to constructor the What's new PR query.
/// </summary>
/// <param name="owner">The owner org of the repo</param>
/// <param name="repository">The name of the repository</param>
/// <param name="branch">The base branch for PRs</param>
/// <param name="labels">Any labels to check for</param>
/// <param name="dateRange">The data range to process</param>
public readonly record struct WhatsNewVariables(
    string owner, 
    string repository,
    string branch, 
    IEnumerable<string>? labels, 
    DateRange dateRange);

/// <summary>
/// This encapsulates the PullRequest node from a 
/// GraphQL query.
/// </summary>
/// <remarks>
/// This readonly struct provides easy access
/// to the properties of the PR.
/// </remarks>
public sealed record WhatsNewPullRequest : IssueOrPullRequest, IGitHubQueryResult<WhatsNewPullRequest, WhatsNewVariables>
{
    private const string PRQueryText = """
        query PullRequestsMerged ($search_value: String!, $cursor: String) {
            search(query: $search_value, type: ISSUE, first: 25, after: $cursor) {
                pageInfo {
                    hasNextPage
                    endCursor
                }
                nodes {
                    ... on PullRequest {
                        title
                        number
                        changedFiles
                        id
                        url
                        labels(first: 5) {
                            nodes {
                                name
                            }
                        }
                        author {
                            login
                            ... on User {
                                name
                            }
                        }
                    }
                }
            }
        }
        """;

    public static GraphQLPacket GetQueryPacket(WhatsNewVariables variables, bool isScalar)
    {
        if (isScalar) throw new InvalidOperationException("This query is not a scalar query");

        var labelFilter = variables.labels?.Any() == true
            ? string.Join(" ", variables.labels)
            : string.Empty;
        var search_value = $"type:pr is:merged base:{variables.branch} {labelFilter} " +
            $"repo:{variables.owner}/{variables.repository} " +
            $"closed:{variables.dateRange.StartDate:yyyy-MM-dd}..{variables.dateRange.EndDate:yyyy-MM-dd}";
        var queryText = new GraphQLPacket
        {
            query = PRQueryText,
            variables = { [nameof(search_value)] = search_value }
        };
        return queryText;
    }

    public static IEnumerable<string> NavigationToNodes(bool isScalar) => ["search"];

    public static WhatsNewPullRequest? FromJsonElement(JsonElement element, WhatsNewVariables variables) => new WhatsNewPullRequest(element);

    /// <summary>
    /// Construct the PullRequest from the Json node.
    /// </summary>
    /// <param name="pullRequestNode"></param>
    private WhatsNewPullRequest(JsonElement pullRequestNode) : base(pullRequestNode)
    {
        Url = pullRequestNode.GetProperty("url").GetString() ?? string.Empty;
        ChangedFiles = pullRequestNode.GetProperty("changedFiles").GetInt32();
        Author = Actor.FromJsonElement(pullRequestNode.GetProperty("author"));
        Labels = (from label in pullRequestNode.Descendent("labels", "nodes").EnumerateArray()
                 select label.GetProperty("name").GetString()).ToArray();

    }

    /// <summary>
    /// Retrieve the Url property.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Access the number of changed files.
    /// </summary>
    public int ChangedFiles { get; }

    /// <summary>
    /// Access the author object for this PR.
    /// </summary>
    public Actor? Author { get; }

    /// <summary>
    /// Retrun the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels { get; }
}

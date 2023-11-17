using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The record that contains the variables
/// </summary>
/// <param name="Organization">The GitHub organization.</param>
/// <param name="Repository">The GitHub repository.</param>
public readonly record struct BankruptcyIssueVariables(string Organization, string Repository);

/// <summary>
/// This struct represents an issue returned
/// from a query.
/// </summary>
/// <remarks>
/// Because many different queries return issues,
/// not all fields may be filled in on each query.
/// </remarks>
public record BankruptcyIssue : Issue, IGitHubQueryResult<BankruptcyIssue, BankruptcyIssueVariables>
{
    private const string OpenIssuesForBankruptcyQuery = """
        query FindIssuesForBankruptcyQuery($organization: String!, $repository: String!, $cursor: String){
          repository(owner:$organization, name:$repository) {
            issues(first:25, after: $cursor, states:OPEN) {
              pageInfo {
                hasNextPage
                endCursor
              }
              nodes {
                id
                number
                title
                author {
                  login
                }
                createdAt
                body
                labels(first:25) {
                  nodes {
                    name
                  }
                }
              }
            }
          }
        }
        """;

    public static BankruptcyIssue FromJsonElement(JsonElement element) => 
        new(element);

    public static GraphQLPacket GetQueryPacket(BankruptcyIssueVariables variables) =>
        new()
        {
            query = OpenIssuesForBankruptcyQuery,
            variables =
            {
                ["organization"] = variables.Organization,
                ["repository"] = variables.Repository,
            }
        };

    public BankruptcyIssue(JsonElement element) : base(element)
    {
        Id = ResponseExtractors.GetIdValue(element);
        Title = ResponseExtractors.GetTitleValue(element);
        Author = Actor.FromJsonElement(ResponseExtractors.GetAuthorChildElement(element));
        Labels = ResponseExtractors.GetChildArrayNames(element);
        Body = ResponseExtractors.GetBodyValue(element);
        CreatedDate = ResponseExtractors.GetCreatedAtValue(element);
    }

    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// The title of the issue.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The author of this issue
    /// </summary>
    public Actor? Author { get; }

    /// <summary>
    /// Return the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels { get; }
    
    /// <summary>
    /// The body of the issue
    /// </summary>
    public string? Body { get; }

    /// <summary>
    /// Retrieve the date time created
    /// </summary>
    public DateTime CreatedDate { get; }
}

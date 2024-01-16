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
sealed public record BankruptcyIssue : Issue, IGitHubQueryResult<BankruptcyIssue, BankruptcyIssueVariables>
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

    public static GraphQLPacket GetQueryPacket(BankruptcyIssueVariables variables, bool isScalar) => isScalar
        ? throw new InvalidOperationException("This query doesn't support scalar queries")
        : new()
          {
              query = OpenIssuesForBankruptcyQuery,
              variables =
              {
                  ["organization"] = variables.Organization,
                  ["repository"] = variables.Repository,
              }
          };

    public static BankruptcyIssue FromJsonElement(JsonElement element, BankruptcyIssueVariables unused) => 
        new(element);

    public static IEnumerable<string> NavigationToNodes(bool isScalar) =>
        isScalar
            ? throw new InvalidOperationException("This query doesn't support scalar queries")
            : ["repository", "issues"];

    public BankruptcyIssue(JsonElement element) : base(element)
    {
        Author = Actor.FromJsonElement(ResponseExtractors.GetAuthorChildElement(element));
        Labels = ResponseExtractors.GetChildArrayElements(element, "labels", 
            label => ResponseExtractors.StringProperty(label, "name"));
        CreatedDate = ResponseExtractors.GetCreatedAtValue(element);
    }

    /// <summary>
    /// The author of this issue
    /// </summary>
    public Actor? Author { get; }

    /// <summary>
    /// Return the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels { get; }
    
    /// <summary>
    /// Retrieve the date time created
    /// </summary>
    public DateTime CreatedDate { get; }
}

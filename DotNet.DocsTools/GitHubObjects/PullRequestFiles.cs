using DotNetDocs.Tools.GitHubCommunications;
using System.Text;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The variables needed to retrieve the file paths in a pull request.
/// </summary>
/// <param name="owner">The owner organization</param>
/// <param name="repository">The GitHub repository</param>
/// <param name="number">The PR number</param>
public readonly record struct FilesModifiedVariables(
       string owner,
       string repository,
       int number);


/// <summary>
/// The response object for the file paths in a pull request.
/// </summary>
/// <remarks>
/// This type handles the response from the GraphQL query that
/// returns the files in the pull request.
/// </remarks>
public sealed record PullRequestFiles : IGitHubQueryResult<PullRequestFiles, FilesModifiedVariables>
{
    private const string FilesInPRQuery = """
      query FindPRFile($organization: String!, $repository: String!, $prNumber: Int!, $cursor: String) {
        repository(owner: $organization, name: $repository) {
          pullRequest(number: $prNumber) {
            files(first: 25, after: $cursor) {
              pageInfo {
                hasNextPage
                endCursor
              }
              nodes {
                path
              }
            }
          }
        }
      }
      """;

    public static GraphQLPacket GetQueryPacket(FilesModifiedVariables queryVariables, bool isScalar) => isScalar 
        ? throw new InvalidOperationException("This query doesn't support scalar queries")
        : new()
        {
            query = FilesInPRQuery,
            variables = 
            {
                ["organization"] = queryVariables.owner,
                ["repository"] = queryVariables.repository,
                ["prNumber"] = queryVariables.number,
            }
        };

    public static IEnumerable<string> NavigationToNodes(bool isScalar)
    {
        if (isScalar) throw new InvalidOperationException("This query doesn't support scalar queries");
        return ["repository", "pullRequest", "files"];
    }

    public static PullRequestFiles? FromJsonElement(JsonElement element, FilesModifiedVariables variables) =>
        new(ResponseExtractors.StringProperty(element, "path"));

    private PullRequestFiles(string path) => Path = path;

    public string Path { get; }
}

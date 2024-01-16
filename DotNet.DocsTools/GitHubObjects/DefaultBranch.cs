using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public readonly record struct DefaultBranchVariables(string Organization, string Repository);
public record DefaultBranch : IGitHubQueryResult<DefaultBranch, DefaultBranchVariables>
{
    private const string Query = """
      query GetDefaultBranch($organization: String!, $repository: String!) {
        repository(owner: $organization, name: $repository) {
          defaultBranchRef {
            name
          }
        }
      }
      """;

    public static GraphQLPacket GetQueryPacket(DefaultBranchVariables variables, bool isScalar) => isScalar
        ? new()
        {
            query = Query,
            variables =
            {
                ["organization"] = variables.Organization,
                ["repository"] = variables.Repository,
            }
        }
        : throw new InvalidOperationException("This query doesn't support enumerations");

    public static IEnumerable<string> NavigationToNodes(bool isScalar) =>
        isScalar
            ? ["repository", "defaultBranchRef"]
            : throw new InvalidOperationException("This query doesn't support array queries");

    public string DefaultBranchName { get; }

    private DefaultBranch(string defaultBranchName) => DefaultBranchName = defaultBranchName;

    public static DefaultBranch FromJsonElement(JsonElement branchRefNode, DefaultBranchVariables _)
    {
        var branchName = ResponseExtractors.StringProperty(branchRefNode, "name");
        return new DefaultBranch(branchName);
    }
}

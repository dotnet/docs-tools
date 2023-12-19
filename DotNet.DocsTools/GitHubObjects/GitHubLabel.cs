using DotNetDocs.Tools.GitHubCommunications;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// The variables for finding a GitHub label query.
/// </summary>
/// <param name="Organization">The GitHub organization</param>
/// <param name="Repository">The GitHub repository</param>
/// <param name="labelText">The text of the label.</param>
/// <remarks>For labels with emojis, the `:` prefix and suffix may be used to
/// describe the emoji.
/// </remarks>
public readonly record struct FindLabelQueryVariables(string Organization, string Repository, string labelText);

/// <summary>
/// Record type for a GitHub label
/// </summary>
public sealed record GitHubLabel : IGitHubQueryResult<GitHubLabel, FindLabelQueryVariables>
{
    private static readonly string allLabels = """
    query EnumerateLabels($organization: String!, $repository: String!, $cursor:String) {
      repository(owner: $organization, name: $repository) {
        labels(first: 50, after: $cursor) {
          pageInfo {
            hasNextPage
            endCursor
          }
          nodes {
            name
            id
          }
        }
      }
    }
    """;

    private const string findLabel = """
        query FindLabel($labelName: String!, $organization: String!, $repository: String!) {
          repository(owner:$organization, name:$repository) {
            label(name: $labelName) {
              id
              name
            }
          }
        }
        """;

    public static GraphQLPacket GetQueryPacket(FindLabelQueryVariables variables, bool isScalar) => isScalar
        ? new()
        {
            query = findLabel,
            variables =
            {
                ["organization"] = variables.Organization,
                ["repository"] = variables.Repository,
                ["labelName"] = variables.labelText
            }
        }
        : new()
        {
            query = allLabels,
            variables =
            {
                ["organization"] = variables.Organization,
                ["repository"] = variables.Repository
            }
        };

    public static IEnumerable<string> NavigationToNodes(bool isScalar) => ["repository", "label"];

    /// <summary>
    /// Construct a GitHub label from a JsonElement
    /// </summary>
    /// <param name="labelElement"></param>
    /// <exception cref="ArgumentException"></exception>
    private GitHubLabel(string name, string id) =>
        (Name, Id) = (name, id);

    public static GitHubLabel? FromJsonElement(JsonElement element, FindLabelQueryVariables variables) =>
        element.ValueKind switch
        { 
            JsonValueKind.Null => null,
            JsonValueKind.Object => new GitHubLabel(
                name: ResponseExtractors.StringProperty(element, "name"),
                id: ResponseExtractors.StringProperty(element, "id")
            ),
            _ => throw new ArgumentException("Must be an object", nameof(element))
       };

    /// <summary>
    /// The name of the label.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The node ID for the label.
    /// </summary
    /// <remarks>
    /// If it turns out that a common record type can be used for 
    /// all GH objects, this would be in the common base type.
    /// </remarks>
    public string Id { get; }
}


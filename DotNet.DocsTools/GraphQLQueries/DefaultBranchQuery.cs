using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// Query for fetching a repository's default branch name.
/// </summary>
public class DefaultBranchQuery
{
    private readonly IGitHubClient _client;
    private readonly string _organization;
    private readonly string _repository;
    private JsonElement _rootElement;

    private const string Query =
@"query GetDefaultBranch($organization: String!, $repository: String!) {
  repository(owner: $organization, name: $repository) {
    defaultBranchRef {
      name
    }
  }
}";

    /// <summary>
    /// Constructs the GitHub GraphQL API query object.
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    /// <param name="organization">The owner of the repository.</param>
    /// <param name="repository">The repository name.</param>
    public DefaultBranchQuery(
        IGitHubClient client, string organization, string repository)
    {
        _client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        _organization = !string.IsNullOrWhiteSpace(organization)
            ? organization
            : throw new ArgumentException(message: "must not be whitespace", paramName: nameof(organization));
        _repository = !string.IsNullOrWhiteSpace(repository)
            ? repository
            : throw new ArgumentException(message: "must not be whitespace", paramName: nameof(repository));
    }

    /// <summary>
    /// Perform the query.
    /// </summary>
    /// <returns>true if the label was found. False otherwise.</returns>
    public async Task<bool> PerformQuery()
    {
        var fileContentsPacket = new GraphQLPacket
        {
            query = Query,
            variables =
            {
                ["organization"] = _organization,
                ["repository"] = _repository,
            }
        };

        _rootElement = await _client.PostGraphQLRequestAsync(fileContentsPacket);

        return _rootElement.Descendent("repository", "defaultBranchRef").ValueKind switch
        {
            JsonValueKind.Object => true,
            JsonValueKind.Null => false,
            _ => throw new InvalidOperationException($"Unexpected result: {_rootElement}"),
        };
    }

    /// <summary>
    /// Gets the repository's default branch name.
    /// </summary>
    public string DefaultBranch =>
        _rootElement.Descendent("repository", "defaultBranchRef", "name").GetString() ??
        throw new InvalidOperationException("default branch not found");
}

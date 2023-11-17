using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GitHubCommunications;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// This query returns all issues (either closed or open) with a given label.
/// </summary>
/// <remarks>
/// You construct the query with the correct arguments,
/// then run the query.
/// </remarks>
public class EnumerateOpenIssues
{
    private static readonly string enumerateOpenIssues =
@"query FindIssuesWithLabel($organization: String!, $repository: String!, $cursor: String){
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
}";
    private readonly IGitHubClient client;
    private readonly string organization;
    private readonly string repository;

    /// <summary>
    /// Construct the query object.
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    /// <param name="organization">The owner organization</param>
    /// <param name="repository">The repository</param>
    /// <remarks>
    /// If the label contains an emoji, the label text should include the ":" to 
    /// mark the start and end of the emoji text.
    /// </remarks>
    public EnumerateOpenIssues(IGitHubClient client, 
        string organization, string repository)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.organization = !string.IsNullOrWhiteSpace(organization)
            ? organization
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(organization));
        this.repository = !string.IsNullOrWhiteSpace(repository)
            ? repository
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(repository));
    }

    /// <summary>
    /// Run the async query.
    /// </summary>
    /// <returns>The async enumerable.</returns>
    /// <remarks>
    /// This query encapsulates the paging API for GitHub's GraphQL 
    /// endpoint.
    /// </remarks>
    public async IAsyncEnumerable<BankruptcyIssue> PerformQuery()
    {
        var findIssuesPacket = new GraphQLPacket
        {
            query = enumerateOpenIssues,
            variables =
            {
                ["organization"] = organization,
                ["repository"] = repository,
            }
        };

        var cursor = default(string);
        bool hasMore = true;
        while (hasMore)
        {
            findIssuesPacket.variables["cursor"] = cursor!;
            var jsonData = await client.PostGraphQLRequestAsync(findIssuesPacket);

            (hasMore, cursor) = jsonData.Descendent("repository", "issues").NextPageInfo();

            var elements = jsonData.Descendent("repository", "issues", "nodes").EnumerateArray();
            foreach (var item in elements)
                yield return new BankruptcyIssue(item);
        }
    }
}

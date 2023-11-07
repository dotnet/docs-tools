using DotNetDocs.Tools.Utility;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GitHubObjects;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// Retrieve all PRs merged during a sprint
/// </summary>
/// <remarks>
/// This class encapsulates retrieving and enumerating
/// all PRs merged during a given sprint for a single repository.
/// The constructor sets the arguments for the query, and validates
/// all arguments.
/// The Perform Query method starts an iteration of (possibly)
/// multiple requests that would enumerate all PRs merged to the 
/// specified branch during the sprint.
/// </remarks>
public class PullRequestsMergedInSprint
{
    private const string PRQueryText =
        @"query PullRequestsMerged ($search_value: String!, $end_cursor: String) {
              search(query: $search_value, type: ISSUE, first: 25, after: $end_cursor) {
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
            }";

    private readonly IGitHubClient client;
    private readonly string search_value;

    /// <summary>
    /// Construct the object for this query
    /// </summary>
    /// <param name="client">The client object</param>
    /// <param name="owner">The owner of this repo</param>
    /// <param name="repository">The repository name</param>
    /// <param name="branch">The name of the branch within the repository</param>
    /// <param name="labels">A collection of label filters to apply</param>
    /// <param name="dateRange">The range of dates to query</param>
    public PullRequestsMergedInSprint(
        IGitHubClient client, string owner, string repository, 
        string branch, IEnumerable<string>? labels, DateRange dateRange)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(owner));
        if (string.IsNullOrWhiteSpace(repository))
            throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(repository));
        if (string.IsNullOrWhiteSpace(branch))
            throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(branch));

        var labelFilter = labels != null && labels.Any() ? string.Join(" ", labels) : string.Empty;
        search_value = $"type:pr is:merged base:{branch} {labelFilter} " +
                       $"repo:{owner}/{repository} " +
                       $"closed:{dateRange.StartDate:yyyy-MM-dd}..{dateRange.EndDate:yyyy-MM-dd}";
    }

    /// <summary>
    /// Enumerate the Pull Request results
    /// </summary>
    /// <returns>The async enumerable for these PRs</returns>
    /// <remarks>
    /// The GraphQL interface is ideally suited to paging and async
    /// enumeration. So, the query returns the enumerable.
    /// </remarks>
    public async IAsyncEnumerable<PullRequest> PerformQuery()
    {
        var queryText = new GraphQLPacket
        {
            query = PRQueryText,
            variables = { [nameof(search_value)] = search_value }
        };

        var cursor = default(string);
        var hasMore = true;
        while (hasMore)
        {
            queryText.variables["end_cursor"] = cursor!;
            Console.WriteLine($"Sending query:\n{queryText.ToJsonText()}\n\n");
            var jsonData = await client.PostGraphQLRequestAsync(queryText);

            var searchNodes = jsonData.GetProperty("search");

            Console.WriteLine($"Query response:\n{jsonData.ToString()}");

            foreach (var item in searchNodes.GetProperty("nodes").EnumerateArray())
                yield return new PullRequest(item);
            (hasMore, cursor) = searchNodes.NextPageInfo();
        }
    }
}

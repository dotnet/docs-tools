using DotNetDocs.Tools.GitHubCommunications;

namespace DotNetDocs.Tools.GraphQLQueries;


/// <summary>
/// retrieve all file paths in a pull request.
/// </summary>
public class FilesInPullRequest
{
    private const string FilesInPRQuery =
@"query FindPRFile($owner_name: String!, $repo: String!, $prNumber:Int!, $cursor:String) {
  repository(owner: $owner_name, name: $repo) {
    pullRequest(number: $prNumber)
        {
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
}";
    private readonly IGitHubClient client;
    private readonly string owner;
    private readonly string repo;
    private readonly int number;

    /// <summary>
    /// Constructor. Set the client and arguments for the query
    /// </summary>
    /// <param name="client">The client object</param>
    /// <param name="owner">The owner of the repository</param>
    /// <param name="repo">The respository name</param>
    /// <param name="number">The PR number.</param>
    public FilesInPullRequest(IGitHubClient client, string owner, string repo, int number)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.owner = !string.IsNullOrWhiteSpace(owner) 
            ? owner
            : throw new ArgumentException(message: "must not be whitespace", paramName: nameof(owner));
        this.repo = !string.IsNullOrWhiteSpace(repo)
            ? repo
            : throw new ArgumentException(message: "must not be whitespace", paramName: nameof(repo));
        this.number = (number > 0) 
            ? number
            : throw new ArgumentException(message: "Must be a positive number", paramName: nameof(number));
    }

    /// <summary>
    /// Perform the query.
    /// </summary>
    /// <returns>The async enumerable for the pasth to the files</returns>
    /// <remarks>
    /// This query enumerates all the paths for changed files in a PR
    /// </remarks>
    public async IAsyncEnumerable<string> PerformQuery()
    {
        var queryText = new GraphQLPacket
        {
            query = FilesInPRQuery,
            variables =
            {
                ["owner_name"] = owner,
                ["repo"] = repo,
                ["prNumber"] = number

            }
        };

        var cursor = default(string);
        bool hasMore = true;
        while (hasMore)
        {
            queryText.variables["cursor"] = cursor!;
            var jsonData = await client.PostGraphQLRequestAsync(queryText);


            var filesNode = jsonData.Descendent("repository", "pullRequest", "files");
            (hasMore, cursor) = filesNode.NextPageInfo();

            var elements = filesNode.GetProperty("nodes").EnumerateArray();
            foreach (var item in elements)
                yield return item.GetProperty("path").GetString() ?? throw new InvalidOperationException("path element not present");
        }
    }
}

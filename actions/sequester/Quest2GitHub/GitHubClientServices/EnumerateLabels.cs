using DotNet.DocsTools.GitHubObjects;

namespace DotNetDocs.Tools.GraphQLQueries;

/// <summary>
/// This query returns a single label nodeID
/// </summary>
/// <remarks>
/// This query object is constructed, then the query is performed. If the label
/// is found, the label can be retrieved from the LabelID property.
/// </remarks>
public class EnumerateLabels
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

    private readonly IGitHubClient client;
    private readonly string organization;
    private readonly string repository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="client">The GitHub client.</param>
    /// <param name="organization">The organization name.</param>
    /// <param name="repository">The repository name.</param>
    /// <remarks>
    /// When the label contains emojis, the label text should use the `:` text indication
    /// of the emoji label.
    /// </remarks>
    public EnumerateLabels(IGitHubClient client, string organization, string repository)
    {
        this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
        this.organization = !string.IsNullOrWhiteSpace(organization)
            ? organization
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(organization));
        this.repository = !string.IsNullOrWhiteSpace(repository)
            ? repository
            : throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(repository));
    }

    public async IAsyncEnumerable<GitHubLabel> AllLabels()
    {
        var allLabelsPacket = new GraphQLPacket
        {
            query = allLabels,
            variables =
            {
                ["organization"] = organization,
                ["repository"] = repository,
                ["cursor"] = null!,
            }
        };

        var cursor = default(string);
        bool hasMore = true;
        while (hasMore)
        {
            allLabelsPacket.variables[nameof(cursor)] = cursor!;
            var jsonData = await client.PostGraphQLRequestAsync(allLabelsPacket);

            (hasMore, cursor) = jsonData.Descendent("repository", "labels").NextPageInfo();

            var elements = jsonData.Descendent("repository", "labels", "nodes").EnumerateArray();
            foreach (var item in elements)
            {
                yield return new GitHubLabel(item);
            }
        }
    }
}

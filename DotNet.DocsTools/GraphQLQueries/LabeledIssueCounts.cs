using DotNetDocs.Tools.GitHubCommunications;

namespace DotNetDocs.Tools.GraphQLQueries
{
    /// <summary>
    /// Retrieve the count of open issues based on labels
    /// </summary>
    /// <remarks>
    /// This query returns the number of matching open issues. You can specify
    /// either issues with certain labels, or issues without certain labels,
    /// or a mix.
    /// </remarks>
    public class LabeledIssueCounts
    {
        private const string AreaIssuesCount =
@"query ($search_value: String!) {
  search(query: $search_value, type: ISSUE) {
    issueCount
  }
}
";
        private readonly IGitHubClient client;
        private readonly string search_value;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="client">The GitHub client.</param>
        /// <param name="owner">The owner of the repository</param>
        /// <param name="repository">The repository name</param>
        /// <param name="labelFilter">The GraphQL string for filtering</param>
        public LabeledIssueCounts(IGitHubClient client, string owner, string repository, string labelFilter)
        {
            this.client = client ?? throw new ArgumentNullException(paramName: nameof(client), message: "Cannot be null");
            if (string.IsNullOrWhiteSpace(owner))
                throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(owner));
            if (string.IsNullOrWhiteSpace(repository))
                throw new ArgumentException(message: "Must not be whitespace", paramName: nameof(repository));
            search_value = $"repo:{owner}/{repository} is:issue is:open " + labelFilter;
            //Console.WriteLine(search_value);
        }

        /// <summary>
        /// Perform the query for the issue count
        /// </summary>
        /// <returns>The number of open issues matching this query.</returns>
        public async Task<int> PerformQueryAsync()
        {
            var queryText = new GraphQLPacket
            {
                query = AreaIssuesCount
            };
            queryText.variables["search_value"] = search_value;

            var results = await client.PostGraphQLRequestAsync(queryText);
            return results.Descendent("search", "issueCount").GetInt32();
        }
    }
}

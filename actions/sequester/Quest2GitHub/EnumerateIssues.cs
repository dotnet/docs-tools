namespace Quest2GitHub;

public class EnumerateIssues
{
    private static readonly string enumerateOpenIssues = """
        query FindIssuesWithLabel($organization: String!, $repository: String!, $cursor: String){
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
                state
                author {
                  login
                  ... on User {
                    name
                  }
                }
                projectsV2 {
                  totalCount
                }
                projectItems {
                  totalCount
                }
                bodyHTML
                body
                assignees(first: 10) {
                  nodes {
                    login
                    ... on User {
                      name
                    }          
                  }
                }
                labels(first: 10) {
                  nodes {
                    name
                    id
                  }
                }
                comments(first: 25) {
                  nodes {
                    author {
                      login
                      ... on User {
                        name
                      }
                    }
                    bodyHTML
                  }
                }
              }
            }
          }
        }
        """;

    public async IAsyncEnumerable<GithubIssue> AllOpenIssue(IGitHubClient client, string organization, string repository)
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
            findIssuesPacket.variables[nameof(cursor)] = cursor!;
            var jsonData = await client.PostGraphQLRequestAsync(findIssuesPacket);

            (hasMore, cursor) = jsonData.Descendent("repository", "issues").NextPageInfo();

            var elements = jsonData.Descendent("repository", "issues", "nodes").EnumerateArray();
            foreach (var item in elements)
                yield return GithubIssue.FromJson(item, organization, repository);
        }
    }
}

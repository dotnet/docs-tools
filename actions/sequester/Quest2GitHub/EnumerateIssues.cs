using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub;

public class EnumerateIssues
{
    private static readonly string enumerateUpdatedQuestIssues = """
        query FindUpdatedIssues($organization: String!, $repository: String!, $questlabels: [String!], $cursor: String) {
          repository(owner: $organization, name: $repository) {
            issues(
              first: 25
              after: $cursor
              labels: $questlabels
              orderBy: {
                field: UPDATED_AT, 
                direction: DESC
              }
            ) {
              pageInfo {
                hasNextPage
                endCursor
              }
              nodes {
                id
                number
                title
                state
                updatedAt
                author {
                  login
                  ... on User {
                    name
                  }
                }
                projectItems(first: 25) {
                  ... on ProjectV2ItemConnection {
                    nodes {
                      ... on ProjectV2Item {
                        fieldValues(first:10) {
                          nodes {
                            ... on ProjectV2ItemFieldSingleSelectValue {
                              field {
                                ... on ProjectV2FieldCommon {
                                  name
                                }
                              }
                              name
                            }
                          }
                        }
                      }
                      project {
                        ... on ProjectV2 {
                          title
                        }
                      }
                    }
                  }
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
                labels(first: 15) {
                  nodes {
                    name
                    id
                  }
                }
                comments(first: 50) {
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

    public async IAsyncEnumerable<QuestIssue> AllQuestIssues(IGitHubClient client, 
        string organization, string repository, 
        string importTriggerLabelText, string importedLabelText)
    {
        var findIssuesPacket = new GraphQLPacket
        {
            query = enumerateUpdatedQuestIssues,
            variables =
            {
                ["organization"] = organization,
                ["repository"] = repository,
                ["questlabels"] = new string[]
                {
                    importTriggerLabelText,
                    importedLabelText
                }
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
            {
                // TODO:
                yield return default; // QuestIssue.FromJsonElement(item, organization, repository);
            }
        }
    }
}

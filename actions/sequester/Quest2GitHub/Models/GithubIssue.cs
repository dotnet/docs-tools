namespace Quest2GitHub.Models;

/// <summary>
/// Simple record type for a GitHub label
/// </summary>
/// <param name="name">The text of the label</param>
/// <param name="nodeID">the unique node ID for this label</param>
public record GitHubLabel(string name, string nodeID);

/// <summary>
/// Model for a GitHub issue
/// </summary>
/// <remarks>
/// This class represents a Github issue, including
/// the fields needed for linking with Quest.
/// </remarks>
public class GithubIssue
{
    private const string queryText = """
    query IssueDetails($owner_name: String!, $repo: String!, $issueNumber:Int!) {
      repository(owner: $owner_name, name: $repo) {
        issue(number: $issueNumber) {
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
          labels(first: 100) {
            nodes {
              name
              id
            }
          }
          comments(first: 100) {
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
    """;

    /// <summary>
    /// The GitHub node id (not the issue number)
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The issue number.
    /// </summary>
    public required int IssueNumber { get; init; }

    /// <summary>
    /// True if the issue is open.
    /// </summary>
    public required bool IsOpen { get; init; }

    /// <summary>
    /// The title of the issue.
    /// </summary>
    // TODO: Should this sync on edits?
    public required string Title { get; init; }

    /// <summary>
    /// The issue author.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// The body of the issue, formatted as HTML
    /// </summary>
    public required string? BodyHtml { get; init; }

    /// <summary>
    /// The body of the issue, as markdown.
    /// </summary>
    public required string? Body { get; init; }

    /// <summary>
    /// The list of assignees. Empty if unassigned.
    /// </summary>
    public required string[] Assignees { get; init; }

    /// <summary>
    /// The list of labels. Empty if unassigned.
    /// </summary>
    public required GitHubLabel[] Labels { get; init; }

    /// <summary>
    /// The list of comments. Empty if there are no comments.
    /// </summary>
    /// <remarks>
    /// The tuple includes the author and the body, formatted as HTML.
    /// </remarks>
    public required (string author, string bodyHTML)[] Comments { get; init; }

    /// <summary>
    /// The link text to this GH Issue.
    /// </summary>
    /// <remarks>
    /// the link text is formatted HTML for the link to the issue.
    /// </remarks>
    public required string LinkText { get; init; }

    /// <summary>
    /// Has this issue been added to a project?
    /// </summary>
    /// <remarks>
    /// This includes closed projects. At this point
    /// I haven't found an API to find only open projects.
    /// </remarks>
    public required bool InProjects { get; init; }

    /// <summary>
    /// Retrieve an issue
    /// </summary>
    /// <param name="client">The Github client service.</param>
    /// <param name="ghOrganization">The organization.</param>
    /// <param name="ghRepository">The repository.</param>
    /// <param name="ghIssueNumber">The issue number</param>
    /// <returns>That task that will produce the issue
    /// when the task is completed.
    /// </returns>

    public static async Task<GithubIssue> QueryIssue(IGitHubClient client, 
        string ghOrganization, string ghRepository, int ghIssueNumber)
    {
        var packet = new GraphQLPacket
        {
            query = queryText,
            variables =
            {
                ["owner_name"] = ghOrganization,
                ["repo"] = ghRepository,
                ["issueNumber"] = ghIssueNumber,
            }
        };

        var rootElement = await client.PostGraphQLRequestAsync(packet);

        var issueNode = rootElement.Descendent("repository", "issue");
        return FromJson(issueNode, ghOrganization, ghRepository);
    }

    public static GithubIssue FromJson(
        JsonElement issueNode, 
        string ghOrganization,
        string ghRepository)
    {
        var id = issueNode.Descendent("id").GetString()!;
        var number = issueNode.Descendent("number").GetInt32();
        var authorNode = issueNode.Descendent("author", "login");
        var author = authorNode.ValueKind is JsonValueKind.String ?
            authorNode.GetString()! : "Ghost";
        var authorNameNode = issueNode.Descendent("author", "name");
        author += authorNameNode.ValueKind is System.Text.Json.JsonValueKind.String ?
            $" - {authorNameNode.GetString()!}" : "";
        var title = issueNode.Descendent("title").GetString();
        bool isOpen = issueNode.Descendent("state").GetString() is "OPEN";
        var bodyText = issueNode.Descendent("bodyHTML").GetString();
        var bodyMarkdown = issueNode.Descendent("body").GetString();
        var numberProjects = issueNode.Descendent("projectsV2", "totalCount").GetInt32() +
            issueNode.Descendent("projectItems", "totalCount").GetInt32();

        var assignees = from item in issueNode.Descendent("assignees").GetProperty("nodes").EnumerateArray()
                        select item.GetProperty("login").GetString();
        var labels = from item in issueNode.Descendent("labels").GetProperty("nodes").EnumerateArray()
                     select new GitHubLabel(item.GetProperty("name").GetString()!, item.GetProperty("id").GetString()!);
        var comments = from item in issueNode.Descendent("comments").GetProperty("nodes").EnumerateArray()
                       let element = item.Descendent("author", "login")
                       select (
                         element.ValueKind is JsonValueKind.String ?
                            element.GetString()! : "Ghost",
                         item.GetProperty("bodyHTML").GetString()
                       );

        return new GithubIssue
        {
            LinkText = $"""
            <a href = "https://github.com/{ghOrganization}/{ghRepository}/issues/{number}">
              {ghOrganization}/{ghRepository}#{number}
            </a>
            """,
            Id = id,
            IssueNumber = number,
            IsOpen = isOpen,
            Title = title!,
            Author = author,
            BodyHtml = bodyText,
            Body = bodyMarkdown,
            Assignees = assignees.ToArray(),
            Labels = labels.ToArray(),
            Comments = comments.ToArray(),
            InProjects = numberProjects > 0,
        };
    }


    /// <summary>
    /// Retrieve the assigned name, if an MS employee
    /// </summary>
    /// <param name="ospoClient">The Open Source program office client service.</param>
    /// <returns>The email address of the assignee</returns>
    public async Task<string?> AuthorMicrosoftEmailAddress(OspoClient ospoClient)
    {
        var identity = await ospoClient.GetAsync(Author);
        return identity?.MicrosoftInfo?.EmailAddress;
    }

    /// <summary>
    /// Retrieve the assigned name, if an MS employee
    /// </summary>
    /// <param name="ospoClient">The Open Source program office client service.</param>
    /// <returns>The email address of the assignee. Null if unassigned, or the assignee is not a 
    /// Microsoft FTE.</returns>
    public async Task<string?> AssignedMicrosoftEmailAddress(OspoClient ospoClient)
    {
        if (Assignees.Any())
        {
            var identity = await ospoClient.GetAsync(Assignees.First());
            return identity?.MicrosoftInfo?.EmailAddress;
        }
        return null;
    }

    /// <summary>
    /// Format this item as a string.
    /// </summary>
    /// <returns>A multi-line formatted string for display.</returns>
    public override string ToString()
    {
        return $$"""
        Issue Number: {{IssueNumber}} - {{Title}}
        {{BodyHtml}}
        Added to a project: {{InProjects}}
        Open: {{IsOpen}}
        Assignees: {{String.Join(", ", Assignees)}}
        Labels: {{String.Join(", ", Labels.Select(l => l.name))}}
        Comments:
        {{String.Join("\n\n", from c in Comments select $"Author: {c.author}\nText:\n{c.bodyHTML}")}}
        """;
    }
}


using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;


// Questions: Can this type implement both a scalar and an enumeration static abstract interface?
// Will overrides work, or are different method names required?


/// <summary>
/// Model for a GitHub issue
/// </summary>
/// <remarks>
/// This class represents a Github issue, including
/// the fields needed for linking with Quest.
/// </remarks>
public class QuestIssue
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
          timelineItems(last: 5) {
            nodes {
              ... on ClosedEvent {
                closer {
                  ... on PullRequest {
                    url
                  }
                }
              }
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
                  project {
                    ... on ProjectV2 {
                      title
                    }
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
    public required int Number { get; init; }

    /// <summary>
    /// The title of the issue.
    /// </summary>
    // TODO: Should this sync on edits?
    public required string Title { get; init; }

    /// <summary>
    /// The body of the issue, as markdown.
    /// </summary>
    public required string? Body { get; init; }

    /// <summary>
    /// True if the issue is open.
    /// </summary>
    public required bool IsOpen { get; init; }

    /// <summary>
    /// The issue author.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// The body of the issue, formatted as HTML
    /// </summary>
    public required string? BodyHtml { get; init; }

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

    public required DateTime UpdatedAt { get; init; }

    /// <summary>
    /// Pairs of Project name, story point size values
    /// </summary>
    public required IEnumerable<StoryPointSize> ProjectStoryPoints { get; init; }

    /// <summary>
    /// The Closing PR (if the issue is closed)
    /// </summary>
    public required string? ClosingPRUrl { get; init; }

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
    public static async Task<QuestIssue> QueryIssue(IGitHubClient client, 
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
        return FromJsonElement(issueNode, ghOrganization, ghRepository);
    }

    public static QuestIssue FromJsonElement(
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
        DateTime udpateTime = issueNode.TryGetProperty("updatedAt"u8, out var updated) ? updated.GetDateTime() : DateTime.Now;

        var assignees = from item in issueNode.Descendent("assignees").GetProperty("nodes").EnumerateArray()
                        select item.GetProperty("login").GetString();
        var labels = from item in issueNode.Descendent("labels").GetProperty("nodes").EnumerateArray()
                     select new GitHubLabel(item);
        var comments = from item in issueNode.Descendent("comments").GetProperty("nodes").EnumerateArray()
                       let element = item.Descendent("author", "login")
                       select (
                         element.ValueKind is JsonValueKind.String ?
                            element.GetString()! : "Ghost",
                         item.GetProperty("bodyHTML").GetString()
                       );

        var projectData = issueNode.Descendent("projectItems", "nodes");
        var storyPoints = new List<StoryPointSize>();
        if (projectData.ValueKind is JsonValueKind.Array)
        {
            foreach (var projectItem in issueNode.Descendent("projectItems", "nodes").EnumerateArray())
            {
                StoryPointSize? sz = StoryPointSize.OptionalFromJsonElement(projectItem);
                if (sz is not null) storyPoints.Add(sz);
            }
        }
        // Timeline events are in order, so the last PR is the most recent closing PR
        var timeline = issueNode.Descendent("timelineItems", "nodes");
        var closedEvent = (timeline.ValueKind == JsonValueKind.Array) ?
            timeline.EnumerateArray()
            .LastOrDefault(t =>
            (t.TryGetProperty("closer", out var closer) &&
            closer.ValueKind == JsonValueKind.Object))
            : default;
        // check state. If re-opened, don't reference the (not correct) closing PR
        string? closingPR = ((closedEvent.ValueKind == JsonValueKind.Object) && !isOpen)
            ? closedEvent.Descendent("closer", "url").GetString()
            : default;

        return new QuestIssue
        {
            LinkText = $"""
            <a href = "https://github.com/{ghOrganization}/{ghRepository}/issues/{number}">
              {ghOrganization}/{ghRepository}#{number}
            </a>
            """,
            Id = id,
            Number = number,
            IsOpen = isOpen,
            Title = title!,
            Author = author,
            BodyHtml = bodyText,
            Body = bodyMarkdown,
            Assignees = assignees.ToArray(),
            Labels = labels.ToArray(),
            Comments = comments.ToArray(),
            UpdatedAt = udpateTime,
            ProjectStoryPoints = storyPoints,
            ClosingPRUrl = closingPR,
        };
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
            // This feels like a hack, but it is necessary.
            // The email address is the email address a person configured
            // However, the only guaranteed way to find the person in Quest 
            // is to use their alias as an email address. Yuck.
            if (identity?.MicrosoftInfo?.EmailAddress?.EndsWith("@microsoft.com") == true)
                return identity.MicrosoftInfo.Alias + "@microsoft.com";
            else
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
        Issue Number: {{Number}} - {{Title}}
        {{BodyHtml}}
        Open: {{IsOpen}}
        Assignees: {{String.Join(", ", Assignees)}}
        Labels: {{String.Join(", ", Labels.Select(l => l.Name))}}
        Sizes: {{String.Join("\n", from sp in ProjectStoryPoints select $"Month, Year: {sp.Month}, {sp.CalendarYear}  Size: {sp.Size}")}}
        Comments:
        {{String.Join("\n\n", from c in Comments select $"Author: {c.author}\nText:\n{c.bodyHTML}")}}
        """;
    }
}


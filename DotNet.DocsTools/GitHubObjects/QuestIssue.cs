using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using Microsoft.DotnetOrg.Ospo;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public readonly record struct QuestIssueVariables(string Organization, string Repository, int issueNumber);

// Questions: Can this type implement both a scalar and an enumeration static abstract interface?
// Will overrides work, or are different method names required?


/// <summary>
/// Model for a GitHub issue
/// </summary>
/// <remarks>
/// This class represents a Github issue, including
/// the fields needed for linking with Quest.
/// </remarks>
public sealed record QuestIssue : Issue, IGitHubQueryResult<QuestIssue, QuestIssueVariables>
{
    private const string QuestIssuesQueryText = """
    query GetIssueForQuestImport($organization: String!, $repository: String!, $issueNumber:Int!) {
      repository(owner: $organization, name: $repository) {
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

    public static GraphQLPacket GetQueryPacket(QuestIssueVariables variables) =>
        new()
        {
            query = QuestIssuesQueryText,
            variables =
            {
                ["organization"] = variables.Organization,
                ["repository"] = variables.Repository,
                ["issueNumber"] = variables.issueNumber,
            }
        };

    public static QuestIssue FromJsonElement(JsonElement issueNode, QuestIssueVariables variables) =>
        new QuestIssue(issueNode, variables.Organization, variables.Repository);

    // TODO: Leverage ResponseExtractors
    public QuestIssue(JsonElement issueNode, string organization, string repository) : base(issueNode)
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

        this.LinkText = $"""
        <a href = "https://github.com/{organization}/{repository}/issues/{number}">
            {organization}/{repository}#{number}
        </a>
        """;
        this.IsOpen = isOpen;
        this.Author = author;
        this.BodyHtml = bodyText;
        this.Assignees = assignees.ToArray();
        this.Labels = labels.ToArray();
        this.Comments = comments.ToArray();
        this.UpdatedAt = udpateTime;
        this.ProjectStoryPoints = storyPoints;
        this.ClosingPRUrl = closingPR;
    }

    /// <summary>
    /// True if the issue is open.
    /// </summary>
    public bool IsOpen { get; }

    /// <summary>
    /// The issue author.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// The body of the issue, formatted as HTML
    /// </summary>
    public string? BodyHtml { get; }

    /// <summary>
    /// The list of assignees. Empty if unassigned.
    /// </summary>
    public string[] Assignees { get; }

    /// <summary>
    /// The list of labels. Empty if unassigned.
    /// </summary>
    public GitHubLabel[] Labels { get; }

    /// <summary>
    /// The list of comments. Empty if there are no comments.
    /// </summary>
    /// <remarks>
    /// The tuple includes the author and the body, formatted as HTML.
    /// </remarks>
    public (string author, string bodyHTML)[] Comments { get; }

    /// <summary>
    /// The link text to this GH Issue.
    /// </summary>
    /// <remarks>
    /// the link text is formatted HTML for the link to the issue.
    /// </remarks>
    public string LinkText { get; }

    public DateTime UpdatedAt { get; }

    /// <summary>
    /// Pairs of Project name, story point size values
    /// </summary>
    public IEnumerable<StoryPointSize> ProjectStoryPoints { get; }

    /// <summary>
    /// The Closing PR (if the issue is closed)
    /// </summary>
    public string? ClosingPRUrl { get; }

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


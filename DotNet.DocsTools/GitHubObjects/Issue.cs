using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNetDocs.Tools.GitHubObjects;

/// <summary>
/// This struct represents an issue returned
/// from a query.
/// </summary>
/// <remarks>
/// Because many different queries return issues,
/// not all fields may be filled in on each query.
/// </remarks>
public readonly struct Issue
{
    private readonly JsonElement element;

    /// <summary>
    /// Construct an issue from the JsonElement.
    /// </summary>
    /// <param name="element">The element</param>
    public Issue(JsonElement element) => this.element = element;

    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    public string Id => element.GetProperty("id").GetString()!;

    /// <summary>
    /// The title of the issue.
    /// </summary>
    public string Title => element.GetProperty("title").GetString()!;

    /// <summary>
    /// The URL for the issue.
    /// </summary>
    public string Url => element.GetProperty("url").GetString()!;

    /// <summary>
    /// The author of this issue
    /// </summary>
    public Actor Author => new Actor(element.GetProperty("author"));

    /// <summary>
    /// Retrieve the issue number.
    /// </summary>
    public int Number => element.GetProperty("number").GetInt32();

    /// <summary>
    /// Enumerate all assignees
    /// </summary>
    public IEnumerable<string> Assignees => from assignee in element
                                            .Descendent("assignees", "nodes")
                                                .EnumerateArray()
                                            select assignee.GetProperty("login").GetString();

    /// <summary>
    /// Return the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels => from label in element
                                         .Descendent("labels", "nodes")
                                             .EnumerateArray()
                                         select label.GetProperty("name").GetString();

    /// <summary>
    /// Return the list of project associations for this issue
    /// </summary>
    public IEnumerable<string> ProjectCards => from card in element
                                               .Descendent("projectCards", "nodes")
                                               .EnumerateArray()
                                               select card.GetProperty("id").GetString();


    /// <summary>
    /// Retrieve the name of the closer, if stored.
    /// </summary>
    public Actor? Closer
    {
        get
        {
            var closedEvent = element.Descendent("timeline", "nodes").EnumerateArray()
                .FirstOrDefault(t =>
                (t.TryGetProperty("closer", out var closer) &&
                closer.ValueKind == JsonValueKind.Object));

            Actor? closer = null;
            if (closedEvent.ValueKind == JsonValueKind.Object)
            {
                if (closedEvent.TryGetProperty("closer", out var pr) &&
                    pr.TryGetProperty("author", out var author))
                {
                    closer = new Actor(author);
                }
            }
            return closer;
        }
    }

    /// <summary>
    /// The body of the issue
    /// </summary>
    public string? Body => element.GetProperty("body").GetString();

    /// <summary>
    /// Retrieve the date time created
    /// </summary>
    public DateTime CreatedDate => element.GetProperty("createdAt").GetDateTime();
}

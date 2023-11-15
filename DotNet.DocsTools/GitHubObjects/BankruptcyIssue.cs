using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// This struct represents an issue returned
/// from a query.
/// </summary>
/// <remarks>
/// Because many different queries return issues,
/// not all fields may be filled in on each query.
/// </remarks>
public class BankruptcyIssue(JsonElement element) : Issue(element)
{
    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    public string Id => element.GetProperty("id").GetString()!;

    /// <summary>
    /// The title of the issue.
    /// </summary>
    public string Title => element.GetProperty("title").GetString()!;

    /// <summary>
    /// The author of this issue
    /// </summary>
    public Actor Author => new Actor(element.GetProperty("author"));

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
    /// The body of the issue
    /// </summary>
    public string? Body => element.GetProperty("body").GetString();

    /// <summary>
    /// Retrieve the date time created
    /// </summary>
    public DateTime CreatedDate => element.GetProperty("createdAt").GetDateTime();
}

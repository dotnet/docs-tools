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
    public string Id { get; } = ResponseExtractors.GetIdValue(element);

    /// <summary>
    /// The title of the issue.
    /// </summary>
    public string Title { get; } = ResponseExtractors.GetTitleValue(element);

    /// <summary>
    /// The author of this issue
    /// </summary>
    public Actor? Author { get; } = Actor.FromJsonElement(ResponseExtractors.GetAuthorChildElement(element));

    /// <summary>
    /// Return the list of labels on this issue.
    /// </summary>
    public IEnumerable<string> Labels { get; } = ResponseExtractors.GetChildArrayNames(element);
    
    /// <summary>
    /// The body of the issue
    /// </summary>
    public string? Body { get; } = ResponseExtractors.GetBodyValue(element);

    /// <summary>
    /// Retrieve the date time created
    /// </summary>
    public DateTime CreatedDate { get; } = ResponseExtractors.GetCreatedAtValue(element);
}

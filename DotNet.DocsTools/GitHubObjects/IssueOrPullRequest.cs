using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

/// <summary>
/// This is the base class for any query result representing an issue or pull request.
/// </summary>
/// <remarks>
/// GitHub's object model has a common type for issue and PR, so we probably should too.
/// This represents that common base type.
/// </remarks>
public abstract record IssueOrPullRequest
{
    /// <summary>
    /// Construct this record from the JsonElement.
    /// </summary>
    /// <param name="element">The Json object that represents the issue or PR.</param>
    public IssueOrPullRequest(JsonElement element)
    {
        Id = ResponseExtractors.GetIdValue(element);
        Number = element.GetProperty("number").GetInt32();
        Title = ResponseExtractors.GetTitleValue(element);
    }

    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    /// <remarks>
    /// Every GitHub object has a unique node ID. This is used to identify the issue. 
    /// It may be that every query uses the nodeID. I haven't verified that yet. If that
    /// turns out to be true, then this property should be moved to the base class.
    /// </remarks>
    public string Id { get; }

    /// <summary> 
    /// Retrieve the issue or PR number.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// The title of the issue or PR.
    /// </summary>
    public string Title { get; }
}

using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public abstract record Issue
{
    public Issue(JsonElement element)
    {
        Id = ResponseExtractors.GetIdValue(element);
        Number = element.GetProperty("number").GetInt32();
        Title = ResponseExtractors.GetTitleValue(element);
        Body = ResponseExtractors.GetBodyValue(element);
    }

    /// <summary>
    /// The node ID for the issue.
    /// </summary>
    public string Id { get; }

    /// <summary> 
    /// Retrieve the issue number.
    /// </summary>
    public int Number { get; }

    /// <summary>
    /// The title of the issue.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The body of the issue
    /// </summary>
    public string? Body { get; }
}

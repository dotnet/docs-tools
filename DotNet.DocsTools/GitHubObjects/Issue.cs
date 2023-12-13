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
    /// <remarks>
    /// Every GitHub object has a unique node ID. This is used to identify the issue. 
    /// It may be that every query uses the nodeID. I haven't verified that yet. If that
    /// turns out to be true, then this property should be moved to the base class.
    /// </remarks>
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

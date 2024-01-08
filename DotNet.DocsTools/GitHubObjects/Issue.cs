using System.Text.Json;

namespace DotNet.DocsTools.GitHubObjects;

public abstract record Issue : IssueOrPullRequest
{ 
    public Issue(JsonElement element) : base(element)
    {
        Body = ResponseExtractors.GetBodyValue(element);
    }

    /// <summary>
    /// The body of the issue
    /// </summary>
    /// <remarks>
    /// This is the body of the issue as markdown text.
    /// </remarks>
    public string? Body { get; }
}

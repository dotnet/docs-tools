namespace DotnetDocsTools.GitHubCommunications;

/// <summary>
/// This internal class is a simple record type
/// that defines the packet to convert markdown to HTML.
/// </summary>
class MarkdownToHtmlRequest
{
    /// <summary>
    /// Markdown text
    /// </summary>
    public string? text { get; set; }

    /// <summary>
    /// Markdown text as Json.
    /// </summary>
    /// <returns>The Json text.</returns>
    public string ToJsonText() => JsonSerializer.Serialize(this);
}

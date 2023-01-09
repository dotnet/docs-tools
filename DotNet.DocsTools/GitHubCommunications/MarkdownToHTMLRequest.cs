using System.Text.Json;

namespace DotnetDocsTools.GitHubCommunications;

/// <summary>
/// This internal class is a simple record type
/// that defines the packet to convert markdown to HTML.
/// </summary>
class MarkdownToHtmlRequest
{
    public string? text { get; set; }
    public string ToJsonText() => JsonSerializer.Serialize(this);
}

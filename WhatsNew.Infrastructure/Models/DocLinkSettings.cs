namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// Encapsulates the settings that control the construction of doc 
/// links in the generated Markdown file.
/// </summary>
public class DocLinkSettings
{
    /// <summary>
    /// The link format to use for links to docs appearing in the 
    /// generated Markdown file.
    /// </summary>
    public LinkFormat LinkFormat { get; init; }

    /// <summary>
    /// The path that prefixes the doc link.
    /// </summary>
    /// <remarks>
    /// This property is required when <see cref="LinkFormat" /> 
    /// is <see cref="LinkFormat.Relative"/>.
    /// </remarks>
    /// <example>/dotnet/</example>
    public string? RelativeLinkPrefix { get; init; }
}

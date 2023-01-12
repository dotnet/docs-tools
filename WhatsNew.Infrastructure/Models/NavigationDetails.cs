namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// This class defines the properties needed to update the TOC.YML and Index.YML files.
/// </summary>
/// <remarks>
/// These properties are used when you run the tool to create a PR for
/// the what's new documents.
/// </remarks>
public class NavigationDetails
{
    /// <summary>
    /// Maximum number of articles live in the TOC.
    /// </summary>
    public int MaximumNumberOfArticles { get; set; }

    /// <summary>
    /// The name of the parent node in the TOC.
    /// </summary>
    public string TocParentNode { get; set; } = default!;

    /// <summary>
    /// Path from the root of the repository to the toc to modify
    /// </summary>
    public string RepoTocFolder { get; set; } = default!;

    /// <summary>
    /// The name of the parent node in the TOC.
    /// </summary>
    public string IndexParentNode { get; set; } = default!;

    /// <summary>
    /// Path from the root of the repository to the toc to modify
    /// </summary>
    public string RepoIndexFolder { get; set; } = default!;
}

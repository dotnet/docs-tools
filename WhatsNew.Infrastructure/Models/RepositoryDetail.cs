using static WhatsNew.Infrastructure.Constants;

namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// Encapsulates necessary details for the docset for which a "What's New"
/// Markdown file is to be generated.
/// </summary>
public class RepositoryDetail
{
    /// <summary>
    /// The name of the branch to process.
    /// </summary>
    public string Branch { get; set; } = null!;

    /// <summary>
    /// The product name supported by the docset.
    /// </summary>
    /// <example>ASP.NET Core</example>
    public string DocSetProductName { get; init; } = null!;

    /// <summary>
    /// The GitHub repository name.
    /// </summary>
    /// <example>AspNetCore.Docs</example>
    public string Name { get; set; } = null!;

    /// <summary>
    /// A flag indicating whether the repository is private.
    /// </summary>
    public bool IsPrivateRepo => Name.EndsWith(PrivateRepoNameSuffix);

    /// <summary>
    /// The GitHub organization name.
    /// </summary>
    /// <example>dotnet</example>
    public string Owner { get; set; } = null!;

    /// <summary>
    /// The GitHub repository's root directory path containing the docs.
    /// </summary>
    /// <example>articles/</example>
    public string RootDirectory { get; init; } = null!;

    /// <summary>
    /// Settings to control the construction of doc links in the generated
    /// Markdown file.
    /// </summary>
    public DocLinkSettings DocLinkSettings { get; init; } = new();

    /// <summary>
    /// The repository directories of interest.
    /// </summary>
    /// <remarks>
    /// Each directory will have a heading printed in the generated 
    /// Markdown file.
    /// </remarks>
    public IEnumerable<RepositoryArea> Areas { get; init; } = null!;

    /// <summary>
    /// Configuration settings that control which content is included in 
    /// and excluded from the generated Markdown file.
    /// </summary>
    public InclusionCriteria InclusionCriteria { get; init; } = new();

    /// <summary>
    /// Optional object that defines config for updating the TOC and Index YAML files.
    /// </summary>
    public NavigationDetails? NavigationOptions { get; set; }
}

/// <summary>
/// An enumeration of link formats supported in the generated Markdown file.
/// </summary>
public enum LinkFormat
{
    /// <summary>
    /// A link in the format `[DOC-TITLE-HERE](../index.md)`.
    /// </summary>
    Relative,
    /// <summary>
    /// A link in the format `[DOC-TITLE-HERE](/site/index)`.
    /// </summary>
    SiteRelative,
    /// <summary>
    /// A link in the format `&lt;xref:index&gt;`.
    /// </summary>
    Xref,
}

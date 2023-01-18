using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.Utility;
using Microsoft.DotnetOrg.Ospo;

namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// Encapsulates all the configuration details necessary to generate 
/// a "What's New" Markdown file.
/// </summary>
public class WhatsNewConfiguration
{
    /// <summary>
    /// Details from the JSON configuration file.
    /// </summary>
    public RepositoryDetail Repository { get; init; } = null!;


    /// <summary>
    /// The start and end dates used for the search.
    /// </summary>
    public DateRange DateRange { get; init; }

    /// <summary>
    /// The displayed date range.
    /// </summary>
    public string RangeTitle { get; init; } = null!;

    /// <summary>
    /// The name to be used for the generated Markdown file.
    /// </summary>
    public string MarkdownFileName { get; init; } = null!;

    /// <summary>
    /// The directory path to which the generated Markdown file should be written.
    /// </summary>
    public string? SaveDir { get; set; }

    /// <summary>
    /// The GitHub client object used for querying.
    /// </summary>
    public IGitHubClient GitHubClient { get; init; } = null!;

    /// <summary>
    /// The path to the root of the repo. 
    /// </summary>
    /// <remarks>
    /// This might eventually replace the saveDir option.
    /// </remarks>
    public string PathToRepoRoot { get; set; } = null!;

    /// <summary>
    /// This is the client that makes queries to the Microsoft OSPO office API.
    /// </summary>
    public OspoClient OspoClient { get; set; } = null!;
}

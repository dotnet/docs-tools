namespace WhatsNew.Infrastructure.Models;

/// <summary>
/// Configuration settings that control which content is included in 
/// and excluded from the generated Markdown file.
/// </summary>
public class InclusionCriteria
{
    /// <summary>
    /// A collection of internal, Microsoft-owned organization names
    /// whose members should be excluded from the community contributors
    /// list.
    /// </summary>
    public List<string> AdditionalMicrosoftOrgs { get; init; } = new();

    /// <summary>
    /// A flag indicating whether to display PR titles.
    /// </summary>
    public bool OmitPullRequestTitles { get; init; }

    /// <summary>
    /// A collection of regular expressions matching PR titles to ignore.
    /// PRs with titles matching the regex(es) will be excluded from the
    /// generated Markdown file.
    /// </summary>
    public List<string> PullRequestTitlesToIgnore { get; init; } = new();

    /// <summary>
    /// A collection of label filters to be applied.
    /// </summary>
    /// <remarks>The label filters will be converted to a space-delimited string.</remarks>
    public List<string> Labels { get; init; } = new();

    /// <summary>
    /// The maximum number of changed files that a PR can contain 
    /// before being ignored.
    /// </summary>
    /// <remarks>The default value is 75.</remarks>
    public int MaxFilesChanged { get; init; } = 75;

    /// <summary>
    /// The minimum number of lines changed that a PR file must 
    /// contain before being included.
    /// </summary>
    /// <remarks>The default value is 75.</remarks>
    public int MinAdditionsToFile { get; init; } = 75;
}

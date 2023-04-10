namespace PullRequestSimulations;

/// <summary>
/// A data type for the data.json file. A result from scanning a pull request change item.
/// </summary>
public readonly struct ExpectedResult
{
    /// <summary>
    /// The result code from the scanner.
    /// </summary>
    /// <remarks>These map to the constant values defined in the <see cref="Snippets5000.DiscoveryResult"/> type.</remarks>
    public int ResultCode { get; init; }

    /// <summary>
    /// The path to a project file
    /// </summary>
    public string DiscoveredProject { get; init; }

    public ExpectedResult(int resultCode, string discoveredProject)
    {
        ResultCode = resultCode;
        DiscoveredProject = discoveredProject;
    }
}

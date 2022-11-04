namespace Quest2GitHub.Options;

public sealed record class AzureDevOpsOptions
{
    /// <summary>
    /// The Azure DevOps organization that serves as an import target.
    /// Defaults to <c>"msft-skilling"</c>.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__Org</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__Org: 'msft-skilling'
    /// </code>
    /// </remarks>
    public string Org { get; init; } = "msft-skilling";

    /// <summary>
    /// The Azure DevOps project to import into.
    /// Defaults to <c>"Content"</c>.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__Project</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__Project: 'Content'
    /// </code>
    /// </remarks>
    public string Project { get; init; } = "Content";

    /// <summary>
    /// The Azure DevOps area path for where issues are to be imported.
    /// Defaults to <c>"Production\Digital and App Innovation\DotNet and more\dotnet"</c>
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__AreaPath</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__AreaPath: 'Production\Digital and App Innovation\DotNet and more\dotnet'
    /// </code>
    /// </remarks>
    public string AreaPath { get; init; } =
        """Production\Digital and App Innovation\DotNet and more\dotnet""";
}

namespace Quest2GitHub.Options;

public sealed record class AzureDevOpsOptions
{
    /// <summary>
    /// The Azure DevOps organization that serves as an import target.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__Org</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__Org: 'msft-skilling'
    /// </code>
    /// </remarks>
    public string Org { get; init; } = null!;

    /// <summary>
    /// The Azure DevOps project to import into.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__Project</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__Project: 'Content'
    /// </code>
    /// </remarks>
    public string Project { get; init; } = null!;

    /// <summary>
    /// The Azure DevOps area path for where issues are to be imported.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__AreaPath</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__AreaPath: 'Production\Digital and App Innovation\DotNet and more\dotnet'
    /// </code>
    /// </remarks>
    public string AreaPath { get; init; } = null!;

    /// <summary>
    /// Gets the path that identifies the localization area for imported issues.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__AzureDevOps__LocalizationAreaPath</c>:
    /// <code>
    /// env:
    ///   ImportOptions__AzureDevOps__AreaPath: 'Localization'
    /// </code>
    /// </remarks>
    public string LocalizationAreaPath { get; init; } = "Localization";
}

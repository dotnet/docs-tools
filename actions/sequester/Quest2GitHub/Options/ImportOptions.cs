namespace Quest2GitHub.Options;

public sealed record class ImportOptions
{
    /// <summary>
    /// The Azure DevOps options that contains configuration for org, project and area path.
    /// </summary>
    public required AzureDevOpsOptions AzureDevOps { get; init; } = new();

    /// <summary>
    /// The various API keys required to perform the import 
    /// operation from GitHub to the Quest (Azure DevOps).
    /// </summary>
    public ApiKeys? ApiKeys { get; init; }

    /// <summary>
    /// The label used to query for issues that need to be imported. 
    /// Triggers an issue to be imported into Quest. Defaults to <c>🗺️ reQUEST</c>.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ImportTriggerLabel</c>:
    /// <code>
    /// env:  # Defaults to '🗺️ reQUEST'
    ///   ImportOptions__ImportTriggerLabel: ':smile: example'
    /// </code>
    /// If your label has an emoji in it, you must specify this using the GitHub emoji colon syntax:
    /// <a href="https://github.com/ikatyang/emoji-cheat-sheet"></a>
    /// </remarks>
    public required string ImportTriggerLabel { get; init; } = ":world_map: reQUEST";

    /// <summary>
    /// The label used to indicate that an issue has been successfully imported into Quest.
    /// Defaults to <c>📌 seQUESTered</c>.
    /// </summary>
    /// <remarks>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__ImportedLabel</c>:
    /// <code>
    /// env:  # Defaults to '📌 seQUESTered'
    ///   ImportOptions__ImportedLabel: ':smile: example'
    /// </code>
    /// If your label has an emoji in it, you must specify this using the GitHub emoji colon syntax:
    /// <a href="https://github.com/ikatyang/emoji-cheat-sheet"></a>
    /// </remarks>
    public required string ImportedLabel { get; init; } = ":pushpin: seQUESTered";
}

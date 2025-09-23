﻿namespace Quest2GitHub.Options;

public record struct ParentForLabel(string? Label, string? Semester, int ParentNodeId);

public record struct LabelToTagMap(string Label, string Tag);

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

    /// <summary>
    /// The label used to indicate that a previously linked issue should be removed
    /// from Azure Devops.  Defaults to <c>💣 vanQUEST</c>.
    /// </summary>
    /// Assign this from an environment variable with the following key, <c>ImportOptions__UnlinkLabel</c>:
    /// <code>
    /// env:  # Defaults to '💣 vanQUEST'
    ///   ImportOptions__ImportedLabel: ':smile: example'
    /// </code>
    /// If your label has an emoji in it, you must specify this using the GitHub emoji colon syntax:
    /// <a href="https://github.com/ikatyang/emoji-cheat-sheet"></a>
    /// </remarks>
    public string UnlinkLabel { get; init; } = ":bomb: vanQUEST";

    /// <summary>
    /// The set of labels where specific parent nodes should be used.
    /// </summary>
    /// <remarks>
    /// When a work item gets created, or updated, it should have
    /// the correct parent. Some labels in a repo will use specific parents.
    /// This is a list of pairs rather than a dictionary because we want them
    /// ordered. The first label found on an issue will be used for the parent.
    /// </remarks>
    public List<ParentForLabel> ParentNodes { get; init; } = [];

    /// <summary>
    /// The default parent node ID to use when no other parent has been configured.
    /// </summary>
    public int DefaultParentNode { get; init; } = 0;

    /// <summary>
    /// A map of GitHub labels to Azure DevOps tags.
    /// </summary>
    /// <remarks>
    /// If an issue has the matching label, add the corresponding tag to
    /// the mapped AzureDevOps item.
    /// </remarks>
    public List<LabelToTagMap> WorkItemTags { get; init; } = [];

    /// <summary>
    /// The only set of GitHub logins that we monitor
    /// </summary>
    /// <remarks>
    /// This set of logins are the only set that we expect
    /// to see for importing Azure DevOps items. Items assigned to
    /// others won't be imported.
    /// <p>
    /// In time, these ids should be stored in a global config
    /// object. In the short term, this is quicker for testing
    /// to ensure that it avoid our rate limit issues.
    /// </p>
    /// </remarks>
    public List<string> TeamGitHubLogins { get; init; } =
        [
        "BillWagner",
        "tdykstra",
        "IEvangelist",
        "gewarren",
        "meaghanlewis",
        "cmastr",
        "adegeo",
        "wadepickett"
        ];

    /// <summary>
    /// This tag is added to issues that are assigned to Copilot.
    /// </summary>
    /// <remarks>
    /// The human assignee is assigned to review and prompt Copilot to perform
    /// the toil of the work.
    /// </remarks>
    public string CopilotIssueTag { get; init; } = "Assignee-Copilot";
}

namespace DotNetDocs.RepoMan.GitHubCommands;

/// <summary>
/// A cache of operations to perform on a GitHub issue or pull request.
/// </summary>
public class OperationPool
{
    /// <summary>
    /// Labels to add to the issue or pull request.
    /// </summary>
    public List<string> LabelsAdd { get; } = [];

    /// <summary>
    /// Labels to remove from the issue or pull request.
    /// </summary>
    public List<string> LabelsRemove { get; } = [];

    /// <summary>
    /// The assignees to set on the issue or pull request.
    /// </summary>
    public List<string> Assignees { get; } = [];

    /// <summary>
    /// The reviewers to set on the pull request.
    /// </summary>
    public List<string> Reviewers { get; } = [];
}

namespace PullRequestSimulations;

/// <summary>
/// A data type for the data.json file. Represents a pull request change item action.
/// </summary>
public enum ChangeItemType
{
    /// <summary>
    /// Create a file.
    /// </summary>
    Create,

    /// <summary>
    /// Modify a file.
    /// </summary>
    Edit,

    /// <summary>
    /// Remove a file.
    /// </summary>
    Delete
}
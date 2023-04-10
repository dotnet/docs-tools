namespace PullRequestSimulations;

/// <summary>
/// A data type for the data.json file. Represents a pull request change item request.
/// </summary>
public readonly struct ChangeItem
{
    /// <summary>
    /// The type of change associated with this item.
    /// </summary>
    public ChangeItemType ItemType { get; init; }

    /// <summary>
    /// The path to the file affected by this item.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    /// Creates a new change item with the specified values.
    /// </summary>
    /// <param name="itemType">The type of change.</param>
    /// <param name="path">The path to the file affected by this item.</param>
    public ChangeItem(ChangeItemType itemType, string path)
    {
        ItemType = itemType;
        Path = path;
    }
}

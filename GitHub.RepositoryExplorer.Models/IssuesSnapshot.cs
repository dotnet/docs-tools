namespace GitHub.RepositoryExplorer.Models;

/// <summary>
/// A snapshot representation of issues.
/// </summary>
/// <param name="Product">The product (<c>ms.prod</c>) value.</param>
/// <param name="Technology">The technology (<c>ms.technology</c>) value.</param>
/// <param name="Priority">The priority value, for example; <c>Pri1</c>.</param>
/// <param name="Classification">The product (<c>ms.prod</c>) value.</param>
/// <param name="DailyCount">The number of issues for a given snapshot.</param>
/// <param name="StartDate">The date of the snapshot.</param>
/// <param name="Interval">The interval between snapshot counts, defaults to one day.</param>
public readonly record struct IssuesSnapshot(
    string? Product,
    string? Technology,
    string? Priority,
    string? Classification,
    int[] DailyCount,
    string StartDate,
    int Interval = 1)
{
    public static implicit operator SnapshotKey(IssuesSnapshot snapshot) =>
        new (
            Product: snapshot.Product,
            Technology: snapshot.Technology,
            Priority: snapshot.Priority,
            Classification: snapshot.Classification);

}
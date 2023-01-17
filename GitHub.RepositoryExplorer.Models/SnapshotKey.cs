namespace GitHub.RepositoryExplorer.Models;

public readonly record struct SnapshotKey(
    string? Product,
    string? Technology,
    string? Priority,
    string? Classification);

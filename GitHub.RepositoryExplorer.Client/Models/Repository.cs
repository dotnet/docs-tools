public record class Repository(
    string? Org,
    string? Repo)
{
    public bool IsAssigned => Org is { Length: > 0 } && Repo is { Length: > 0 };
}

public record class IssueSummary
{
    public bool IsLoading { get; init; } = true;

    public DateOnly Date { get; init; }

    public IEnumerable<ProductIssueCount> Data { get; init; } =
        Array.Empty<ProductIssueCount>();
}
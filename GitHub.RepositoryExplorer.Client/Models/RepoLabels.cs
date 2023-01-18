public record class RepoLabels
{
    public bool IsLoading { get; init; } = true;

    public IssueClassificationModel IssueClassification { get; init; } = new();
}
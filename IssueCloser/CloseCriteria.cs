namespace IssueCloser;

/// <summary>
/// The data record for matching values against close criteria.
/// </summary>
public record CloseCriteria(Priority PriLabel, bool HasDocIssue, bool IsInternal);

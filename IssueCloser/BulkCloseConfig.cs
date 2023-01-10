namespace IssueCloser;

/// <summary>
/// The data object for close config records
/// </summary>
public record BulkCloseConfig(CloseCriteria Criteria, int AgeInMonths);

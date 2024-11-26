namespace Quest2GitHub.Models;

/// <summary>
/// Extended properties for a Quest issue.
/// </summary>
/// <remarks>
/// We generally need the same properties for any GitHub issue
/// or pull request to add it to Azure DevOps. So, build one
/// constructor that creates them.
///
/// TODO:  REFACTOR SO dotted properties aren't stored directly. Put the IterationSize object
/// in this type and add readonly properties.
/// </remarks>
public record ExtendedIssueProperties(
    string GitHubSize, // The size in GitHub
    int? StoryPoints, // The points in AzDo
    int? Priority, // The priority in AzDo
    QuestIteration? LatestIteration, // The latest iteration attached to this issue. If no iteration, set to current iteration.
    // Set this to the "future" iteration as well, if that's the correct location.
    bool IsPastIteration,
    string Month,
    int CalendarYear,
    IEnumerable<string> Tags);

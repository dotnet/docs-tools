using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub.Models;

/// <summary>
/// Extended properties for a Quest issue.
/// </summary>
/// <remarks>
/// We generally need the same properties for any GitHub issue
/// or pull request to add it to Azure DevOps. So, build one
/// type that holds them. when this type is constructed, it uses
/// the issue or pull request to along with some environment settings
/// to compute the values that should be set on the associated work
/// item. This type centralizes the algorithms to determine the
/// Azure Dev Ops properties for an issue based on the issue
/// properties, the project(s) that the issue is a member of
/// and the environment settings.
/// </remarks>
public class WorkItemProperties
{
    public WorkItemProperties (QuestIssueOrPullRequest issue,
        IEnumerable<QuestIteration> iterations,
        IEnumerable<LabelToTagMap> tags,
        IEnumerable<ParentForLabel> parentNodes)
    {
        StoryPointSize? storySize = LatestStoryPointSize(issue);
        StoryPoints = QuestStoryPoint(storySize) ?? 0;
        Priority = GetPriority(issue, storySize) ?? -1;
        var latestIteration = storySize?.ProjectIteration(iterations) ?? QuestIteration.FutureIteration(iterations);
        if ((storySize?.IsPastIteration == true) && issue.IsOpen)
        {
            latestIteration = QuestIteration.FutureIteration(iterations);
        }

        WorkItemState = (issue.IsOpen, latestIteration.Name) switch
        {
            (false, _) => "Closed",
            (true, "Future") => "New",
            (_) => "Committed"
        };
        IterationPath = latestIteration.Path;

        ParentNodeId = 0;

        if (WorkItemState is not "New")
        {
            foreach (ParentForLabel pair in parentNodes)
            {
                if (issue.Labels.Any(l => l.Name == pair.Label) || (pair.Label is null))
                {
                    if ((pair.Semester is null) || (latestIteration.IsInSemester(pair.Semester) is true))
                    {
                        ParentNodeId = pair.ParentNodeId;
                        break;
                    }
                }
            }
        }

        Tags = WorkItemTagsForIssue(issue, tags);

        string month = storySize?.Month ?? "Unknown";
        int calendarYear = storySize?.CalendarYear ?? 0;
        string gitHubSize = storySize?.Size ?? "Unknown";
        IssueLogString = $"GH sprint: {month}-{calendarYear}, size: {gitHubSize}, Iteration: {IterationPath}, State: {WorkItemState}";
    }

    /// <summary>
    /// The story points for the work item. 0 is not set.
    /// </summary>
    public int StoryPoints { get; }

    /// <summary>
    /// The work item priority. -1 is not set.
    /// </summary>
    public int Priority { get; }

    /// <summary>
    /// The state for the workitem.
    /// </summary>
    /// <remarks>
    /// The value is based on the GitHub issue state, and the
    /// correct iteration. If the issue is closed, the work item
    /// state is "Closed". If the issue is open and the item
    /// should be in a sprint, the state is "Committed". Otherwise,
    /// the state is "New".
    /// </remarks>
    public string WorkItemState { get; }

    /// <summary>
    /// The sequence of Azure DevOps tags to apply to the
    /// associated work item.
    /// </summary>
    public IEnumerable<string> Tags { get; }

    /// <summary>
    /// The path to the iteration for this work item.
    /// </summary>
    public string IterationPath { get; }

    /// <summary>
    /// The parent node ID for this work item. 0 means no parent
    /// </summary>
    public int ParentNodeId { get; }

    /// <summary>
    /// A string that can be used to log the iteration values for the work item.
    /// </summary>
    public string IssueLogString { get; }

    private static StoryPointSize? LatestStoryPointSize(QuestIssueOrPullRequest issue)
    {
        IEnumerable<StoryPointSize> sizes = from size in issue.ProjectStoryPoints
                                            let month = StoryPointSize.MonthOrdinal(size.Month)
                                            orderby size.CalendarYear descending,
                                            month descending
                                            select size;

        return sizes.FirstOrDefault();
    }

    private static int? QuestStoryPoint(StoryPointSize? storyPointSize)
    {
        if (storyPointSize?.Size?.Contains("Tiny") == true)
        {
            return 1;
        }
        else if (storyPointSize?.Size?.Contains("Small") == true)
        {
            return 3;
        }
        else if (storyPointSize?.Size?.Contains("Medium") == true)
        {
            return 5;
        }
        else if (storyPointSize?.Size?.Contains("Large") == true)
        {
            return 8;
        }
        else if (storyPointSize?.Size?.Contains("X-Large") == true)
        {
            return 13;
        }
        return null;
    }

    private static int? GetPriority(QuestIssueOrPullRequest issue, StoryPointSize? storySize)
    {
        if (storySize?.Priority is not null) return storySize.Priority;

        // Well, check for priority on the issue itself:
        foreach (var label in issue.Labels)
        {
            //Start at 1, because DevOps uses 1 - 4.
            if (label.Name.StartsWith("P", true, null))
            {
                if (label.Name.Contains("0")) return 1;
                if (label.Name.Contains("1")) return 2;
                if (label.Name.Contains("2")) return 3;
                if (label.Name.Contains("3")) return 4;
            }
        }
        return default;
    }

    private static IEnumerable<string> WorkItemTagsForIssue(QuestIssueOrPullRequest issue, IEnumerable<LabelToTagMap> tags)
    {
        foreach (var label in issue.Labels)
        {
            var tag = tags.FirstOrDefault(t => t.Label == label.Name);
            if (tag.Tag is not null)
            {
                yield return tag.Tag;
            }
        }
    }
}

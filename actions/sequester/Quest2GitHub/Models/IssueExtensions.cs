using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub.Models;

public static class IssueExtensions
{
    public static ExtendedIssueProperties ExtendedProperties(this QuestIssueOrPullRequest issue,
        IEnumerable<QuestIteration> iterations,
        IEnumerable<LabelToTagMap> tags,
        IEnumerable<ParentForLabel> parentNodes)
    {
        StoryPointSize? storySize = issue.LatestStoryPointSize();
        string GitHubSize = storySize?.Size ?? "Unknown";
        string month = storySize?.Month ?? "Unknown";
        int calendarYear = storySize?.CalendarYear ?? 0;
        int? storyPoints = storySize?.QuestStoryPoint();
        int? priority = issue.GetPriority(storySize);
        bool isPastIteration = storySize?.IsPastIteration ?? false;

        QuestIteration? iteration = storySize?.ProjectIteration(iterations);

        int parentNodeId = 0;

        if (!isPastIteration)
        {
            foreach (ParentForLabel pair in parentNodes)
            {
                if (issue.Labels.Any(l => l.Name == pair.Label) || (pair.Label is null))
                {
                    if ((pair.Semester is null) || (iteration?.IsInSemester(pair.Semester) is true))
                    {
                        parentNodeId = pair.ParentNodeId;
                        break;
                    }
                }
            }
        }

        IEnumerable<string> workItemTags = issue.WorkItemTagsForIssue(tags);
        return new ExtendedIssueProperties(GitHubSize, storyPoints, priority, iteration, isPastIteration, month, calendarYear, workItemTags, parentNodeId);
    }
    private static StoryPointSize? LatestStoryPointSize(this QuestIssueOrPullRequest issue)
    {
        IEnumerable<StoryPointSize> sizes = from size in issue.ProjectStoryPoints
                    let month = StoryPointSize.MonthOrdinal(size.Month)
                    orderby size.CalendarYear descending,
                    month descending
                    select size;

        return sizes.FirstOrDefault();
    }

    private static int? QuestStoryPoint(this StoryPointSize storyPointSize)
    {
        if (storyPointSize.Size.Contains("Tiny"))
        {
            return 1;
        }
        else if (storyPointSize.Size.Contains("Small"))
        {
            return 3;
        }
        else if (storyPointSize.Size.Contains("Medium"))
        {
            return 5;
        }
        else if (storyPointSize.Size.Contains("Large"))
        {
            return 8;
        }
        else if (storyPointSize.Size.Contains("X-Large"))
        {
            return 13;
        }
        return null;
    }

    private static int? GetPriority(this QuestIssueOrPullRequest issue, StoryPointSize? storySize)
    {
        if (storySize?.Priority is not null) return storySize.Priority;

        // Well, check for priority on the issue itself:
        foreach(var label in issue.Labels)
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

    public static QuestIteration? ProjectIteration(this StoryPointSize storyPoints, IEnumerable<QuestIteration> iterations)
    {
        // New form: Content\Gallium\FY24Q1\07
        string month = storyPoints.Month;
        int calendarYear = storyPoints.CalendarYear;

        return ProjectIteration(month, calendarYear, iterations);
    }


    /// <summary>
    /// Return the project iteration based on the calendar year and month
    /// </summary>
    /// <param name="month">The 3 letter abbreviation for the current month</param>
    /// <param name="calendarYear">The calendar year</param>
    /// <param name="iterations">All iterations</param>
    /// <returns>The current iteration</returns>
    public static QuestIteration? ProjectIteration(string month, int calendarYear, IEnumerable<QuestIteration> iterations)
    {
        if (!StoryPointSize.TryGetMonthOrdinal(month, out int monthNumber))
        {
            return default;
        }
        int fy = ((monthNumber > 6 ? calendarYear + 1 : calendarYear)) % 100;
        string fiscalYearPattern = $"FY{fy:D2}";
        string monthPattern = $"{monthNumber:D2} {month}";

        foreach(QuestIteration iteration in iterations)
        {
            if (iteration.Path.Contains(fiscalYearPattern) && iteration.Path.Contains(monthPattern))
            {
                return iteration;
            }
        }
        return default;
    }

    /// <summary>
    /// Return tags for a given issue
    /// </summary>
    /// <param name="issue">The GitHub issue or pull request</param>
    /// <param name="tags">The mapping from issue to tag</param>
    /// <returns>An enumerable of tags</returns>
    private static IEnumerable<string> WorkItemTagsForIssue(this QuestIssueOrPullRequest issue, IEnumerable<LabelToTagMap> tags)
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

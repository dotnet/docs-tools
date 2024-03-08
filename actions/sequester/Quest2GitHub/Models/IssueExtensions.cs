using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub.Models;

public static class IssueExtensions
{
    private static readonly Dictionary<string, int> s_months = new ()
    {
        ["Jan"] =  1, // 3
        ["Feb"] =  2, // 3
        ["Mar"] =  3, // 3
        ["Apr"] =  4, // 4
        ["May"] =  5, // 4
        ["Jun"] =  6, // 4
        ["Jul"] =  7, // 1
        ["Aug"] =  8, // 1
        ["Sep"] =  9, // 1
        ["Oct"] = 10, // 2
        ["Nov"] = 11, // 2
        ["Dec"] = 12  // 2
    };
    public static StoryPointSize? LatestStoryPointSize(this QuestIssueOrPullRequest issue)
    {
        IEnumerable<StoryPointSize> sizes = from size in issue.ProjectStoryPoints
                    let month = s_months[size.Month]
                    orderby size.CalendarYear descending,
                    month descending
                    select size;

        return sizes.FirstOrDefault();
    }

    public static int? QuestStoryPoint(this StoryPointSize storyPointSize)
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
        if (!s_months.TryGetValue(month, out int monthNumber))
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
}

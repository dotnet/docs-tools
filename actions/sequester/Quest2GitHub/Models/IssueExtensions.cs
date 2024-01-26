using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub.Models;

public static class IssueExtensions
{
    private static readonly Dictionary<string, int> Months = new ()
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
        var sizes = from size in issue.ProjectStoryPoints
                    let month = Months[size.Month]
                    orderby size.CalendarYear descending,
                    month descending
                    select size;

        return sizes.FirstOrDefault();
    }

    public static int? QuestStoryPoint(this StoryPointSize storyPointSize) => 
        storyPointSize.Size.Length switch
        {
            < 6 => null,
            _ => storyPointSize.Size[..6] switch
            {
                "🦔 Tiny" => 1,
                "🐇 Smal" => 3,
                "🐂 Medi" => 5,
                "🦑 Larg" => 8,
                "🐋 X-La" => 13,
                _ => null,
            }
        };

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
        if (!Months.TryGetValue(month, out var monthNumber))
        {
            return default;
        }
        int fy = ((monthNumber > 6 ? calendarYear + 1 : calendarYear)) % 100;
        var fiscalYearPattern = $"FY{fy:D2}";
        var monthPattern = $"{monthNumber:D2} {month}";

        foreach(var iteration in iterations)
        {
            if (iteration.Path.Contains(fiscalYearPattern) && iteration.Path.Contains(monthPattern))
            {
                return iteration;
            }
        }
        return default;
    }
}

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
    public static StoryPointSize? LatestStoryPointSize(this GithubIssue issue)
    {
        var sizes = from size in issue.ProjectStoryPoints
                    let month = Months[size.Month]
                    orderby size.CalendarYear descending,
                    month descending
                    select size;

        return sizes.FirstOrDefault();
    }

    public static int? QuestStoryPoint(this StoryPointSize storyPointSize) =>
        storyPointSize.Size switch
        {
            "🦔 Tiny (4h)" => 1,
            "🐇 Small (1d)" => 3,
            "🐂 Medium (3-5d)" => 5,
            "🦑 Large (10d)" => 8,
            "🐋 X-Large (20d)" => 13,
            _ => null,
        };

    public static QuestIteration? ProjectIteration(this StoryPointSize storyPoints, IEnumerable<QuestIteration> iterations)
    {
        // Old form: Content\CY_2023\03
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
        var oldPattern = $"""CY_{calendarYear:D4}\{Months[month]:D2}""";
        int fy = ((Months[month] > 6 ? calendarYear + 1 : calendarYear)) % 100;
        int q = ((((Months[month]-1) / 3) + 2) % 4) + 1; // Yeah, this is weird. But, it does convert the current month to the FY quarter
        var newPattern = $"""FY{fy:D2}Q{q:D1}\{Months[month]:D2}""";

        foreach(var iteration in iterations)
        {
            if (iteration.Path.Contains(oldPattern) || iteration.Path.Contains(newPattern))
            {
                return iteration;
            }
        }
        return default;
    }
}

using DotNet.DocsTools.GitHubObjects;

namespace Quest2GitHub.Models;

public static class IssueExtensions
{
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
}

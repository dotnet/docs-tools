namespace DotNet.DocsTools.Utility;

/// <summary>
/// Manage start and end dates for a single sprint.
/// </summary>
/// <remarks>
/// Static methods find a sprint based on a date, and an offset from that date.
/// Instance members retrieve the start date, end date, and title for the sprint.
/// </remarks>
public class SprintDateRange
{
    /// <summary>
    /// Retrieve the sprint object for a given date.
    /// </summary>
    /// <param name="selectedDate">The date selected</param>
    /// <returns>A SprintDateRange object for the current sprint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the date is outside of the range of the known sprints.
    /// </exception>
    public static SprintDateRange GetSprintFor(DateTime selectedDate)
    {
        var sprintName = selectedDate.ToString("MMMM yyyy");
        var sprintStartDate = new DateTime(selectedDate.Year, selectedDate.Month, 1);
        var sprintEndDate = sprintStartDate.AddMonths(1).AddDays(-1);
        return new SprintDateRange(sprintName, sprintStartDate, sprintEndDate);
    }

    /// <summary>
    /// Retrieve a sprint based on a date and an offset.
    /// </summary>
    /// <param name="offset">The offset (positive or negative) from the selected date.</param>
    /// <param name="selectedDate">The chosen date to start from.</param>
    /// <returns>A SprintDateRange object that represents the selected sprint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// When the date and offset produce a sprint outside of the known range.
    /// </exception>
    public static SprintDateRange GetOffsetSprintFor(int offset, DateTime selectedDate)
    {
        var startDate = selectedDate.AddMonths(offset);
        return GetSprintFor(startDate);
    }

    private SprintDateRange(string sprintName, DateTime startDate, DateTime endDate)
    {
        SprintName = sprintName;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// Retrieve the name of the sprint
    /// </summary>
    /// <returns></returns>
    public override string ToString() => SprintName;

    /// <summary>
    /// The first date of the sprint.
    /// </summary>
    public DateTime StartDate { get; }

    /// <summary>
    /// The last date of the sprint.
    /// </summary>
    public DateTime EndDate { get; }

    /// <summary>
    /// The name of the sprint, as used in all reporting.
    /// </summary>
    public string SprintName { get; }
}

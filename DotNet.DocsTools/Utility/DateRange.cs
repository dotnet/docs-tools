namespace DotNet.DocsTools.Utility;

/// <summary>
/// Encapsulates the start and end dates for a period of time.
/// </summary>
public readonly struct DateRange
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    /// <summary>
    /// Constructs a date range from the provided <see cref="string"/> values.
    /// </summary>
    /// <param name="startDate">
    /// A <see cref="string"/> representation of the time period's start date.
    /// </param>
    /// <param name="endDate">
    /// A <see cref="string"/> representation of the time period's end date.
    /// </param>
    public DateRange(string startDate, string endDate)
    {
        if (string.IsNullOrWhiteSpace(startDate))
            throw new ArgumentException(
                "The parameter cannot be null, empty, or whitespace.", nameof(startDate));
        if (string.IsNullOrWhiteSpace(endDate))
            throw new ArgumentException(
                "The parameter cannot be null, empty, or whitespace.", nameof(endDate));

        _startDate = DateTime.Parse(startDate);
        _endDate = DateTime.Parse(endDate);

        CompareStartAndEndDates();
    }

    /// <summary>
    /// Constructs a date range from the provided <see cref="DateTime"/> values.
    /// </summary>
    /// <param name="startDate">
    /// A <see cref="DateTime"/> representation of the time period's start date.
    /// </param>
    /// <param name="endDate">
    /// A <see cref="DateTime"/> representation of the time period's end date.
    /// </param>
    public DateRange(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;

        CompareStartAndEndDates();
    }

    /// <summary>
    /// The start date of the time period.
    /// </summary>
    public DateTime StartDate => _startDate;

    /// <summary>
    /// The end date of the time period.
    /// </summary>
    public DateTime EndDate => _endDate;

    private void CompareStartAndEndDates()
    {
        var result = DateTime.Compare(_startDate, _endDate);
        if (result > 0)
            throw new ArgumentOutOfRangeException(
                $"{_startDate:MM/dd/yyyy} occurs after {_endDate:MM/dd/yyyy}.",
                nameof(_startDate).Replace("_", string.Empty));
    }
}

using DotNetDocs.Tools.Utility;
using Xunit;

namespace DotNetDocs.Tools.Tests.Utility;

public class SprintDateRangeTests
{
    // This test acts as a trip-wire for when we need to define
    // more sprints in the SprintDateRange class. When the next 
    // sprint for the following month isn't defined, this test will
    // fail.
    [Fact]
    public void CurrentSprintIsDefined()
    {
        // When you add more sprints to the SprintDateRnage, update this
        // field to the middle of the last month. The test should fail.
        var dateForlastSprint = new DateTime(2022, 12, 15);
        var today = DateTime.Now.Date;
        var currentSprint = SprintDateRange.GetSprintFor(today);
        Assert.NotNull(currentSprint);
        Assert.True(true, $"Current sprint is {currentSprint.SprintName}");
        
        var startDate = currentSprint.StartDate;
        var endDate = currentSprint.EndDate;
        Assert.True(startDate < endDate);

        var finalSprint = SprintDateRange.GetSprintFor(dateForlastSprint);
        startDate = finalSprint.StartDate;
        endDate = finalSprint.EndDate;
        Assert.True(startDate < endDate);
    }

    // Test first, middle, last, date of all sprints.
    // This one is just basic truth for the sprints we've defined so far.
    //This test (while extensive) validates the start and end dates of each sprint object.

    [Theory]
    [InlineData(2020, 12, 28, "December 2020")] // start
    [InlineData(2021, 1, 15, "January 2021")] // middle
    [InlineData(2021, 1, 30, "January 2021")] // end

    [InlineData(2021, 2, 1, "February 2021")] // start
    [InlineData(2021, 2, 15, "February 2021")] // middle
    [InlineData(2021, 2, 27, "February 2021")] // end

    [InlineData(2021, 3, 1, "March 2021")] // start
    [InlineData(2021, 3, 15, "March 2021")] // middle
    [InlineData(2021, 3, 27, "March 2021")] // end

    [InlineData(2021, 3, 29, "March 2021")] // start
    [InlineData(2021, 4, 15, "April 2021")] // middle
    [InlineData(2021, 4, 24, "April 2021")] // end

    [InlineData(2021, 4, 26, "April 2021")] // start
    [InlineData(2021, 5, 15, "May 2021")] // middle
    [InlineData(2021, 5, 29, "May 2021")] // end

    [InlineData(2021, 5, 31, "May 2021")] // start
    [InlineData(2021, 6, 15, "June 2021")] // middle
    [InlineData(2021, 6, 26, "June 2021")] // end

    [InlineData(2021, 6, 28, "June 2021")] // start
    [InlineData(2021, 7, 15, "July 2021")] // middle
    [InlineData(2021, 7, 31, "July 2021")] // end

    [InlineData(2021, 8, 2, "August 2021")] // start
    [InlineData(2021, 8, 15, "August 2021")] // middle
    [InlineData(2021, 8, 28, "August 2021")] // end

    [InlineData(2021, 8, 30, "August 2021")] // start
    [InlineData(2021, 9, 15, "September 2021")] // middle
    [InlineData(2021, 9, 25, "September 2021")] // end

    [InlineData(2021, 9, 27, "September 2021")] // start
    [InlineData(2021, 10, 15, "October 2021")] // middle
    [InlineData(2021, 10, 30, "October 2021")] // end

    [InlineData(2021, 11, 1, "November 2021")] // start
    [InlineData(2021, 11, 15, "November 2021")] // middle
    [InlineData(2021, 11, 27, "November 2021")] // end

    [InlineData(2021, 11, 29, "November 2021")] // start
    [InlineData(2021, 12, 15, "December 2021")] // middle
    [InlineData(2022, 1, 1, "January 2022")] // end
    public void SprintFromDateIsCorrect(int year, int month, int date, string sprintName)
    {
        var sourceDate = new DateTime(year, month, date);
        var sprint = SprintDateRange.GetSprintFor(sourceDate);
        Assert.Equal(sprintName, sprint.SprintName);
    }

    // Check ranges on a sprint:
    [Fact]
    public void SprintRangeIsCorrect()
    {
        var sourceDate = new DateTime(2020, 2, 15);
        var sprint = SprintDateRange.GetSprintFor(sourceDate);
        Assert.Equal("February 2020", sprint.SprintName);
        Assert.Equal(new DateTime(2020, 2, 1), sprint.StartDate);
        Assert.Equal(new DateTime(2020, 2, 29), sprint.EndDate);
    }

    [Theory]
    [InlineData(-1, "November 2019")]
    [InlineData(-2, "October 2019")]
    [InlineData(-3, "September 2019")]
    [InlineData(-4, "August 2019")]
    [InlineData(-5, "July 2019")]
    [InlineData(-6, "June 2019")]
    [InlineData(-7, "May 2019")]
    [InlineData(-8, "April 2019")]
    [InlineData(-9, "March 2019")]
    [InlineData(-10, "February 2019")]
    public void CanNavigatePreviousSprints(int offset, string title)
    {
        var startDate = new DateTime(2019, 12, 21); // pick a day in December 

        var sprint = SprintDateRange.GetOffsetSprintFor(offset, startDate);

        Assert.Equal(title, sprint.SprintName);
    }

    [Theory]
    [InlineData(0, "December 2019")]
    [InlineData(1, "January 2020")]
    [InlineData(2, "February 2020")]
    [InlineData(3, "March 2020")]
    [InlineData(4, "April 2020")]
    [InlineData(5, "May 2020")]
    [InlineData(6, "June 2020")]
    [InlineData(7, "July 2020")]
    [InlineData(8, "August 2020")]
    [InlineData(9, "September 2020")]
    [InlineData(10, "October 2020")]
    [InlineData(11, "November 2020")]
    public void CanNavigateFutureSprints(int offset, string title)
    {
        var startDate = new DateTime(2019, 12, 21); // pick a day in December 

        var sprint = SprintDateRange.GetOffsetSprintFor(offset, startDate);

        Assert.Equal(title, sprint.SprintName);
    }
}

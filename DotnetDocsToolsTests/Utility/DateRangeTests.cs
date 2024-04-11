using DotNetDocs.Tools.Utility;
using Xunit;

namespace DotNetDocs.Tools.Tests.Utility;

public class DateRangeTests
{
    [Theory]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null, "2020-05-31")]
    [InlineData("2020-05-01", null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    [InlineData("", "2020-05-31")]
    [InlineData(" ", "2020-05-31")]
    [InlineData("2020-05-01", "")]
    [InlineData("2020-05-01", " ")]
    public void Empty_Or_Null_Date_Throws_ArgumentException(string startDate, string endDate) =>
        Assert.Throws<ArgumentException>(() => new DateRange(startDate, endDate));

    [Theory]
    [InlineData("2020-111-14", "2020-12-31")]
    [InlineData("2020/11/14", "2020/121/31")]
    public void Invalid_Date_Format_Throws_FormatException(string startDate, string endDate) =>
        Assert.Throws<FormatException>(() => new DateRange(startDate, endDate));

    [Theory]
    [InlineData("6/1/2020", "6/31/2020")]
    [InlineData("6/0/2020", "6/30/2020")]
    public void NonExistent_Date_Throws_FormatException(string startDate, string endDate) =>
        Assert.Throws<FormatException>(() => new DateRange(startDate, endDate));

    [Fact]
    public void Start_Date_After_End_Date_Throws_ArgumentOutOfRangeException()
    {
        DateTime.TryParse("6/30/2020", out var startDate);
        DateTime.TryParse("6/1/2020", out var endDate);

        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new DateRange(startDate.ToString(), endDate.ToString()));
        Assert.Equal($"{nameof(startDate)} (Parameter '{startDate:MM/dd/yyyy} occurs after {endDate:MM/dd/yyyy}.')", exception.Message);
    }
}

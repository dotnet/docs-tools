using WhatsNew.Infrastructure.Models;
using Xunit;

namespace WhatsNew.Infrastructure.Tests.Models;

public class PageGeneratorInputTests
{
    [Theory]
    [InlineData("", "")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null, "")]
    [InlineData("", null)]
    [InlineData("2020-05-01", null)]
    [InlineData(null, "2020-05-31")]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    [InlineData("2020-05-01", "")]
    [InlineData("", "2020-05-31")]
    public void Empty_Or_Null_Date_Throws_ArgumentException(string startDate, string endDate)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PageGeneratorInput
            {
                Owner = "dotnet",
                Repository = "AspNetCore.Docs",
                DateStart = startDate,
                DateEnd = endDate,
            });
        Assert.StartsWith("The parameter cannot be null, empty, or whitespace.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    public void Empty_Or_Null_Owner_Name_Throws_ArgumentException(string ownerName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PageGeneratorInput
            {
                Owner = ownerName,
                DateStart = "2020-05-01",
                DateEnd = "2020-05-31",
            });
        Assert.Equal($"The parameter 'Owner' cannot be null, empty, or whitespace.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
#pragma warning disable xUnit1012 // Null should only be used for nullable parameters
    [InlineData(null)]
#pragma warning restore xUnit1012 // Null should only be used for nullable parameters
    public void Empty_Or_Null_Repo_Name_Throws_ArgumentException(string repoName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new PageGeneratorInput
            {
                Owner = "dotnet",
                Repository = repoName,
                DateStart = "2020-05-01",
                DateEnd = "2020-05-31",
            });
        Assert.Equal($"The parameter 'Repository' cannot be null, empty, or whitespace.", exception.Message);
    }

    [Fact]
    public void Using_Specific_Dates_Has_MonthYear_Null()
    {
        var input = new PageGeneratorInput
        {
            Owner = "dotnet",
            Repository = "docs",
            DateStart = "2020-05-01",
            DateEnd = "2020-05-31",
        };
        Assert.Null(input.MonthYear);
    }
}

using WhatsNew.Infrastructure.Models;
using WhatsNew.Infrastructure.Services;
using Xunit;

namespace WhatsNew.Infrastructure.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly ConfigurationService _service;

    public ConfigurationServiceTests()
    {
        _service = new();
    }

    [Fact(Skip = "Flaky in Az Pipelines build")]
    public async Task Valid_Org_And_Repo_With_Invalid_DocSet_Throws_FileNotFoundException()
    {
        var input = new PageGeneratorInput
        {
            Owner = "dotnet",
            Repository = "docs",
            Branch = "main",
            DocSet = "cognitive-services",
            DateStart = "2020-05-01",
            DateEnd = "2020-05-31",
        };

        var exception = await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.GetConfiguration(input));
        Assert.StartsWith(
            $@"Configuration file '.whatsnew/.{input.DocSet}.json' not found.",
            exception.Message);
    }

    [Theory(Skip = "Flaky in Az Pipelines build")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task Empty_GitHub_Personal_Access_Token_Throws_InvalidOperationException(
        string gitHubKey)
    {
        var input = new PageGeneratorInput
        {
            Owner = "dotnet",
            Repository = "AspNetCore.Docs",
            Branch = "main",
            DateStart = "2020-05-01",
            DateEnd = "2020-05-31",
        };

        Environment.SetEnvironmentVariable("GitHubKey", gitHubKey);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetConfiguration(input));
        Assert.Equal(
            "Store your GitHub personal access token in the 'GitHubKey' environment variable.",
            exception.Message);
    }
}

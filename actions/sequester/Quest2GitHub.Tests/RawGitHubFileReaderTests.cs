namespace Quest2GitHub.Tests;

public class RawGitHubFileReaderTests
{
    [Fact]
    public async Task ReadsImportOptionsCorrectlyTest()
    {
        var sut = new RawGitHubFileReader();
        await sut.TryInitializeOptionsAsync("dotnet", "docs");

        var config = new ConfigurationBuilder()
            .AddJsonFile("quest-config.json")
            .Build()
            .GetSection(nameof(ImportOptions));

        var actual = new ServiceCollection()
            .AddImportServices(config)
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value;

        Assert.NotNull(actual);

        Assert.Equal("msft-skilling", actual.AzureDevOps.Org);
        Assert.Equal("Content", actual.AzureDevOps.Project);
        Assert.Equal(@"Production\Digital and App Innovation\DotNet and more\dotnet", actual.AzureDevOps.AreaPath);

        Assert.Equal(":world_map: reQUEST", actual.ImportTriggerLabel);
        Assert.Equal(":pushpin: seQUESTered", actual.ImportedLabel);
    }
}

namespace Quest2GitHub.Tests;

public class ImportOptionsTests
{
    [Fact]
    public void ImportOptionsDefaultsAreCorrectlyAssignedTest()
    {
        var actual = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value;

        Assert.NotNull(actual);

        // Top-level options
        Assert.Equal(":world_map: reQUEST", actual.ImportTriggerLabel);
        Assert.Equal(":pushpin: seQUESTered", actual.ImportedLabel);
        Assert.Empty(actual.ParentNodes);

        // API keys object
        Assert.Null(actual.ApiKeys);
    }

    [Fact]
    public void ImportOptionsIsBoundToConfigurationTest()
    {
        ArrangeTestVariables();

        var actual = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build()
            .GetRequiredSection(nameof(ImportOptions))
            .Get<ImportOptions>();

        AssertActualOptions(actual);
    }

    [Fact]
    public void ServiceCollectionCorrectlyProvidesOptionsTest()
    {
        ArrangeTestVariables();

        var actual = new ServiceCollection()
            .AddImportServices(new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build()
            .GetRequiredSection(nameof(ImportOptions)))
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value;

        AssertActualOptions(actual);
    }

    [Fact]
    public void ServiceCollectionCorrectlyProvidesJsonOptionsTest()
    {
        var actual = new ServiceCollection()
            .AddImportServices(new ConfigurationBuilder()
            .AddJsonFile("quest-import.json")
            .Build()
            .GetRequiredSection(nameof(ImportOptions)))
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value;

        Assert.NotNull(actual);

        // Top-level options
        Assert.Equal("trigger-import", actual.ImportTriggerLabel);
        Assert.Equal("imported", actual.ImportedLabel);
        Assert.Equal(199082, actual.ParentNodes.Single(p => p.Label == "okr-health").ParentNodeId);
        Assert.Equal(227484, actual.ParentNodes.Single(p => p.Label == "dotnet-csharp/svc").ParentNodeId);

        // Azure DevOps nested options
        Assert.Equal("org", actual.AzureDevOps.Org);
        Assert.Equal("proj", actual.AzureDevOps.Project);
        Assert.Equal("path", actual.AzureDevOps.AreaPath);

        // API keys object
        Assert.Equal("ght", actual.ApiKeys?.GitHubToken);
        Assert.Equal("qkey", actual.ApiKeys?.QuestKey);
    }

    [Fact]
    public void ValidateOptionsTest()
    {
        RemoveEnvVar("GitHubKey");
        RemoveEnvVar("OSPOKey");
        RemoveEnvVar("QuestKey");

        Assert.Throws<ArgumentNullException>(
            () => default(ImportOptions).ValidateOptions());

        var actual = new ServiceCollection()
            .AddOptions()
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value;

        Assert.NotNull(actual);
        Assert.Null(actual.ApiKeys);

        Assert.Throws<ArgumentNullException>(
            () => actual.ValidateOptions());

        actual = actual with
        {
            ApiKeys = new()
            {
                GitHubToken = "", QuestKey = " ", SequesterPrivateKey = " ", SequesterAppID = 0
            }
        };

        Assert.Throws<ArgumentNullException>(
            () => actual.ValidateOptions());
    }

    [Fact]
    public void ImportOptionsSupportSimpleEnvVarKeyNameTest()
    {
        ArrangeEnvVar("GitHubKey", "fake-ght");
        ArrangeEnvVar("OSPOKey", "fake-ospo-key");
        ArrangeEnvVar("QuestKey", "fake-quest-key");

        var actual = new ServiceCollection()
            .AddImportServices(new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build()
            .GetSection(nameof(ImportOptions)))
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ImportOptions>>()
            .Value
            .ValidateOptions();

        Assert.Equal("fake-ght", actual.ApiKeys!.GitHubToken);
        Assert.Equal("fake-quest-key", actual.ApiKeys.QuestKey);
    }

    static void ArrangeEnvVar(string key, string value) =>
        Environment.SetEnvironmentVariable(
            key, value, EnvironmentVariableTarget.Process);

    static void RemoveEnvVar(string key) =>
        Environment.SetEnvironmentVariable(
            key, null, EnvironmentVariableTarget.Process);

    static void ArrangeTestVariables()
    {
        ArrangeEnvVar("ImportOptions__ImportTriggerLabel", "trigger-label");
        ArrangeEnvVar("ImportOptions__ImportedLabel", "imported-label");
        ArrangeEnvVar("ImportOptions__AzureDevOps__Org", "fake-ado-org");
        ArrangeEnvVar("ImportOptions__AzureDevOps__Project", "fake-ado-proj");
        ArrangeEnvVar("ImportOptions__AzureDevOps__AreaPath", "fake-ado-area-path");
        ArrangeEnvVar("ImportOptions__ApiKeys__GitHubToken", "fake-ght");
        ArrangeEnvVar("ImportOptions__ApiKeys__OSPOKey", "fake-ospo-key");
        ArrangeEnvVar("ImportOptions__ApiKeys__QuestKey", "fake-quest-key");
    }

    static void AssertActualOptions(ImportOptions? actual)
    {
        Assert.NotNull(actual);

        // Top-level options
        Assert.Equal("trigger-label", actual.ImportTriggerLabel);
        Assert.Equal("imported-label", actual.ImportedLabel);
        Assert.Empty(actual.ParentNodes);

        // Azure DevOps nested options
        Assert.Equal("fake-ado-org", actual.AzureDevOps.Org);
        Assert.Equal("fake-ado-proj", actual.AzureDevOps.Project);
        Assert.Equal("fake-ado-area-path", actual.AzureDevOps.AreaPath);

        // API keys object
        Assert.Equal("fake-ght", actual.ApiKeys?.GitHubToken);
        Assert.Equal("fake-quest-key", actual.ApiKeys?.QuestKey);
    }
}

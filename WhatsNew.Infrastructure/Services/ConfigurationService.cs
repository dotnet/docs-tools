using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.Utility;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhatsNew.Infrastructure.Models;
using DotNetDocs.Tools.GraphQLQueries;
using DotNet.DocsTools.GitHubObjects;
using Microsoft.DotnetOrg.Ospo;

namespace WhatsNew.Infrastructure.Services;

/// <summary>
/// The class responsible for constructing the configuration details
/// needed to generate "What's New" pages.
/// </summary>
public class ConfigurationService
{
    public async Task<WhatsNewConfiguration> GetConfiguration(PageGeneratorInput input)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var key = config["GitHubKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Store your GitHub personal access token in the 'GitHubKey' environment variable.");

        var dateRange = new DateRange(input.DateStart, input.DateEnd);
        string configFileName, configFileContents, markdownFileName;

        if (string.IsNullOrWhiteSpace(input.DocSet))
        {
            configFileName = ".whatsnew.json";
        }
        else
        {
            configFileName = $".whatsnew/.{input.DocSet.Trim()}.json";
        }

        var client = IGitHubClient.CreateGitHubClient(key);

        var token = config["AZURE_ACCESS_TOKEN"];

        OspoClient? ospoClient = token is not null
            ? new OspoClient(token, true) : null;

        if (ospoClient is null)
        {
            Console.WriteLine("Warning: Microsoft FTEs won't be filtered from the contributor list.");
        }

        if (string.IsNullOrWhiteSpace(input.Branch))
        {
            var query = new ScalarQuery<DefaultBranch, DefaultBranchVariables>(client);
            var result = await query.PerformQuery(new DefaultBranchVariables(input.Owner, input.Repository));

            // There should never be a case where the default branch is null, but just in case...
            if (result is null) throw new InvalidOperationException("Could not find default branch");
            input.Branch = result.DefaultBranchName;
        }

        JToken configToken;

        var pathToConfig = string.IsNullOrWhiteSpace(input.LocalConfig)
            ? Path.Combine(input.RepoRoot, configFileName)
            : input.LocalConfig;

        configFileContents = await File.ReadAllTextAsync(pathToConfig);
        configToken = JToken.Parse(configFileContents);

        var validationService = new SchemaValidationService();
        validationService.ValidateConfiguration(configToken);

        var repo = JsonConvert.DeserializeObject<RepositoryDetail>(configFileContents) ?? throw new InvalidOperationException("Config object not found");
        repo.Branch = input.Branch!;
        repo.Owner = input.Owner;
        repo.Name = input.Repository;

        int mod = repo.NavigationOptions?.MaximumNumberOfArticles ?? 3;
        string fNameCycle = $"mod{dateRange.StartDate.Month % mod}";
        if (string.IsNullOrWhiteSpace(input.DocSet))
        {
            markdownFileName = $"{input.Owner}-{input.Repository}-{fNameCycle}.md";
        }
        else
        {
            markdownFileName = $"{input.Owner}-{input.Repository}-{input.DocSet.Trim()}-{fNameCycle}.md";
        }

        return new WhatsNewConfiguration
        {
            DateRange = dateRange,
            RangeTitle = input.MonthYear ?? $"{input.DateStart} - {input.DateEnd}",
            GitHubClient = client,
            OspoClient = ospoClient,
            MarkdownFileName = markdownFileName,
            Repository = repo,
            SaveDir = input.SaveDir?.Trim(),
            PathToRepoRoot = input.RepoRoot,
        };
    }
}

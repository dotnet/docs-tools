using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.Utility;
using Microsoft.DotnetOrg.Ospo;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WhatsNew.Infrastructure.Models;
using DotNetDocs.Tools.GraphQLQueries;

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
        var ospoKey = config["OspoKey"];
        if (string.IsNullOrWhiteSpace(ospoKey))
            throw new InvalidOperationException("Store your 1ES personal access token in the 'OspoKey' environment variable.");


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

        var ospoClient = new OspoClient(ospoKey, true);
        
        if (string.IsNullOrWhiteSpace(input.Branch))
        {
            var query = new DefaultBranchQuery(client, input.Owner, input.Repository);
            if (await query.PerformQuery())
                input.Branch = query.DefaultBranch;
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
        if (string.IsNullOrEmpty(input.DocSet))
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

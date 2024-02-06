using DotNetDocs.Tools.GitHubCommunications;

internal class Program
{
    /// <summary>
    /// Process issue updates in Azure DevOps - Quest
    /// </summary>
    /// <param name="org">The GitHub organization</param>
    /// <param name="repo">The GutHub repository names.</param>
    /// <param name="issue">The issue number. If null, process all open issues.</param>
    /// <param name="questConfigPath">The config path. If null, use the config file in the root folder of the repository.</param>
    /// <param name="branch">The optional branch to use. Defaults to "main" otherwise.</param>
    /// <param name="duration">For bulk import, how many days past to examine. -1 means all issues. Default is 5</param>
    /// <remarks>
    /// Example command line:
    /// ImportIssues --org dotnet --repo docs --issue 31331
    /// </remarks>
    /// <returns>0</returns>
    private static async Task<int> Main(
        string org,
        string repo,
        int? issue = null,
        int? duration = 5,
        string? questConfigPath = null,
        string? branch = null)
    {
        Console.WriteLine(duration);
        try
        {
            if (repo.Contains('/'))
            {
                string[] split = repo.Split("/");
                repo = split[1];
            }

            branch ??= "main";
            Console.WriteLine($"Using branch: '{branch}'");

            bool singleIssue = (issue is not null && issue.Value != -1);

            Console.WriteLine(singleIssue
                ? $"Processing single issue {issue!.Value}: https://github.com/{org}/{repo}/issues/{issue.Value}"
                : $"Processing all open issues: {org}/{repo}");

            ImportOptions? importOptions;
            if (string.IsNullOrWhiteSpace(questConfigPath) || !File.Exists(questConfigPath))
            {
                using RawGitHubFileReader reader = new();
                importOptions = await reader.ReadOptionsAsync(org, repo, branch);
            }
            else
            {
                LocalFileReader reader = new();
                importOptions = await reader.ReadOptionsAsync(questConfigPath);
            }

            if (importOptions is null)
            {
                throw new ApplicationException(
                    $"Unable to load Quest import configuration options.");
            }

            using QuestGitHubService serviceWorker = await CreateService(importOptions, !singleIssue);

            if (singleIssue)
            {
                await serviceWorker.ProcessIssue(
                    org, repo, issue!.Value); // Odd. There's a warning on issue, but it is null checked above.
            }
            else
            {
                await serviceWorker.ProcessIssues(
                    org, repo, duration ?? -1, true);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"::  -- {ex.Message} -- ");
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }
        return 0;
    }

    private static async Task<QuestGitHubService> CreateService(ImportOptions options, bool bulkImport)
    {
        ArgumentNullException.ThrowIfNull(options.ApiKeys, nameof(options));

        IGitHubClient gitHubClient = (options.ApiKeys.SequesterAppID != 0) 
            ? await IGitHubClient.CreateGitHubAppClient(options.ApiKeys.SequesterAppID, options.ApiKeys.SequesterPrivateKey)
            : IGitHubClient.CreateGitHubClient(options.ApiKeys.GitHubToken);

        return new QuestGitHubService(
                gitHubClient,
                options.ApiKeys.OSPOKey,
                options.ApiKeys.QuestKey,
                options.AzureDevOps.Org,
                options.AzureDevOps.Project,
                options.AzureDevOps.AreaPath,
                options.ImportTriggerLabel,
                options.ImportedLabel,
                bulkImport);
    }
}

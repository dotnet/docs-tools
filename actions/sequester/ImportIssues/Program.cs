internal class Program
{
    /// <summary>
    /// Process issue updates in Azure DevOps - Quest
    /// </summary>
    /// <param name="org">The GitHub organization</param>
    /// <param name="repo">The GutHub repository names.</param>
    /// <param name="issue">The issue number. If null, process all open issues.</param>
    /// <param name="questConfigPath">The config path. If null, use the config file in the root folder of the repository.</param>
    /// <remarks>
    /// Example command line:
    /// ImportIssues --org dotnet --repo docs --issue 31331
    /// </remarks>
    /// <returns>0</returns>
    private static async Task<int> Main(string org, string repo, int? issue = null, string? questConfigPath = null)
    {
        try
        {
            if (repo.Contains('/'))
            {
                var split = repo.Split("/");
                repo = split[1];
            }

            Console.WriteLine(issue.HasValue && issue > -1
                ? $"Processing single issue {issue.Value}: https://github.com/{org}/{repo}/issues/{issue.Value}"
                : "Processing all open issues");

            ImportOptions? importOptions;
            if (string.IsNullOrWhiteSpace(questConfigPath) || !File.Exists(questConfigPath))
            {
                using RawGitHubFileReader reader = new();
                importOptions = await reader.ReadOptionsAsync(org, repo);
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

            using var serviceWorker = new QuestGitHubService(
                importOptions.ApiKeys!.GitHubToken,
                importOptions.ApiKeys.OSPOKey,
                importOptions.ApiKeys.QuestKey,
                importOptions.AzureDevOps.Org,
                importOptions.AzureDevOps.Project,
                importOptions.AzureDevOps.AreaPath,
                importOptions.ImportTriggerLabel,
                importOptions.ImportedLabel);

            if (issue is not null && issue.Value != -1)
            {
                await serviceWorker.ProcessIssue(
                    org, repo, issue.Value);
            }
            else
            {
                await serviceWorker.ProcessIssues(
                    org, repo, false);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 1;
        }

        return 0;
    }
}
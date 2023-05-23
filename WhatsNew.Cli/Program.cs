using WhatsNew.Infrastructure.Models;
using WhatsNew.Infrastructure.Services;

namespace WhatsNew.Cli;

/// <summary>
/// The entry point for the CLI tool.
/// </summary>
public class Program
{
    /// <summary>
    /// Generate the Markdown file for a docs "what's new" page. For usage details, see https://aka.ms/whats-new-tool.
    /// </summary>
    /// <param name="owner">The GitHub organization name.</param>
    /// <param name="repo">The GitHub repository name within the provided organization.</param>
    /// <param name="branch">The branch name within the provided repository.</param>
    /// <param name="docset">The product name within the provided repository.</param>
    /// <param name="startdate">A range start date in a valid format.</param>
    /// <param name="enddate">A range end date in a valid format.</param>
    /// <param name="savedir">An absolute directory path to which the generated Markdown file should be written.</param>
    /// <param name="reporoot">The path to the repository root folder.</param>
    /// <param name="localconfig">An absolute file path to a local JSON configuration file. For local testing only.</param>
    /// <returns>The <see cref="Task"/> generating the what's new page.</returns>
    public static async Task Main(
        string? startdate, string? enddate, string owner, string repo,
        string? branch, string? docset, string? savedir, string? reporoot, string? localconfig,
        string? savefile)
    {
        var today = DateTime.Now;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1)
            .AddMonths(-1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var LastMonth = firstOfMonth.ToString("MMMM yyyy");
        var input = new PageGeneratorInput
        {
            DateStart = string.IsNullOrWhiteSpace(startdate) ? firstOfMonth.ToShortDateString() : startdate,
            DateEnd = string.IsNullOrWhiteSpace(enddate) ? lastOfMonth.ToShortDateString() : enddate,
            MonthYear = string.IsNullOrWhiteSpace(startdate) ? LastMonth : null,
            Owner = owner,
            Repository = repo,
            Branch = branch,
            DocSet = docset,
            SaveDir = savedir,
            RepoRoot = reporoot ?? "./",
            LocalConfig = localconfig,
        };

        var configService = new ConfigurationService();
        var whatsNewConfig = await configService.GetConfiguration(input);
        var pageGenService = new PageGenerationService(whatsNewConfig);

        await pageGenService.WriteMarkdownFile(savefile);
         if (savefile is null)
        {
            var tocService = new TocUpdateService(whatsNewConfig);
            await tocService.UpdateWhatsNewToc();

            var indexService = new IndexUpdateService(whatsNewConfig);
            await indexService.UpdateWhatsNewLandingPage();
        }
        whatsNewConfig.OspoClient.Dispose();
        whatsNewConfig.GitHubClient.Dispose();
    }
}

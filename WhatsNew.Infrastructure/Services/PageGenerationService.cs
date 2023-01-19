﻿using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.Utility;
using System.Text;
using System.Text.RegularExpressions;
using WhatsNew.Infrastructure.Models;
using static WhatsNew.Infrastructure.Constants;
using DotNetDocs.Tools.GraphQLQueries;
using DotNetDocs.Tools.RESTQueries;

namespace WhatsNew.Infrastructure.Services;

public record PrDetail(string Source, bool NewContent, string PrTitle, int PrNumber);

/// <summary>
/// The class responsible for generating monthly "What's New" pages.
/// </summary>
public class PageGenerationService
{
    /*
    File extensions to consider for inclusion in the generated Markdown file.
    While processing PRs, only files with these extensions will be analyzed.
    */
    private readonly List<string> _fileExtensions = new()
    {
        ".md",
        ".yml",
    };
    private readonly List<(string login, string name)> _contributors = new();
    private readonly Dictionary<string, List<PrDetail>> _majorChanges = new();
    private readonly WhatsNewConfiguration _configuration = null!;

    public PageGenerationService(WhatsNewConfiguration configuration) =>
        _configuration = configuration;

    /// <summary>
    /// Generates the "What's New" Markdown file for the docset identified by
    /// <see cref="PageGeneratorInput.Owner"/> and <see cref="PageGeneratorInput.Repo"/>.
    /// </summary>
    public async Task WriteMarkdownFile(TextWriter? textWriter = null)
    {
        await ProcessPullRequests();

        var filePath = GetMarkdownFilePath();
        await using TextWriter stream = textWriter ?? new StreamWriter(filePath);

        await GenerateHeader(stream);
        WriteNewDocInformation(stream);
        await WriteContributorInformation(stream);

        Console.WriteLine($"Created the file \"{filePath}\"");
    }

    private string GetMarkdownFilePath()
    {
        _configuration.SaveDir ??= GetWhatsNewDirectory();
        Directory.CreateDirectory(_configuration.SaveDir);

        var filePath = Path.Combine(_configuration.SaveDir, _configuration.MarkdownFileName);
        return filePath;

        static string GetWhatsNewDirectory()
        {
            // Inspired by the following user secrets configuration provider code:
            // https://github.com/dotnet/runtime/blob/7f7791f5ae3938c643e6c76b514a46b095c1730a/src/libraries/Microsoft.Extensions.Configuration.UserSecrets/src/PathHelper.cs#L46-L51
            var rootDir = Environment.GetEnvironmentVariable("APPDATA")
                ?? Environment.GetEnvironmentVariable("HOME")
                ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrEmpty(rootDir))
                throw new InvalidOperationException(
                    "Could not determine an appropriate location for storing the generated Markdown file. Use the --savedir option to specify a directory where the file should be stored.");

            var whatsNewDir = Path.Combine(rootDir, "whatsnew");
            return whatsNewDir;
        }
    }

    private async Task GenerateHeader(TextWriter stream)
    {
        var docSetProductName = _configuration.Repository.DocSetProductName;

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{docSetProductName} docs: What's new for {_configuration.RangeTitle}\"");
        sb.AppendLine($"description: \"What's new in the {docSetProductName} docs for {_configuration.RangeTitle}.\"");
        sb.AppendLine($"ms.custom: {DateTime.Now.AddMonths(-1):MMMM-yyyy}");
        sb.AppendLine($"ms.date: {DateTime.Now:MM/dd/yyyy}");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {docSetProductName} docs: What's new for {_configuration.RangeTitle}");
        sb.AppendLine();
        sb.AppendLine($"Welcome to what's new in the {docSetProductName} docs for {_configuration.RangeTitle}. This article lists some of the major changes to docs during this period.");
        sb.AppendLine();

        await stream.WriteAsync(sb.ToString());
    }

    private void WriteNewDocInformation(TextWriter stream)
    {
        var repo = _configuration.Repository;
        var allDocs = from change in _majorChanges
                      from area in repo.Areas
                      from areaName in area.Names
                      where change.Key.StartsWith(areaName)
                      orderby area.Heading
                      group change by area.Heading into headingGroup
                      select headingGroup;

        var rootDirectoryHeading = (from area in repo.Areas
                                    where area.Names.FirstOrDefault() == "."
                                    select area.Heading).FirstOrDefault();

        foreach (var docArea in allDocs)
        {
            var areaHeading = docArea.Key;
            var isRootDirectoryArea = areaHeading == rootDirectoryHeading;

            // Don't write anything for blank areas.
            if (areaHeading is null)
                continue;
            stream.WriteLine($"## {areaHeading}");
            stream.WriteLine();

            writeDocNodes(true);
            writeDocNodes(false);

            void writeDocNodes(bool isNew)
            {
                var prQuery = docArea.Where(da => da.Value.Any(pr => pr.NewContent == isNew));

                if (prQuery.Any())
                {
                    stream.WriteLine(isNew ? "### New articles" : "### Updated articles");
                    stream.WriteLine();

                    foreach (var doc in prQuery)
                    {
                        // Potential problem: Why only look at the first source? 
                        List<PrDetail> docPullRequests = doc.Value;
                        // Check all PRs to get a title.
                        // If a single PR title fails, write a warning to the console.
                        // If all PRs fail to retrieve the title, put a warning in the output.
                        var value = docPullRequests.First();
                        string? docLink = default;
                        string prs = "";
                        foreach (var pr in docPullRequests)
                        {
                            // First title wins (it's the newest), but keep checking to issue warnings.
                            docLink ??= getDocLink(pr.Source, doc.Key, isRootDirectoryArea);
                            if (docLink == null)
                            {
                                Console.WriteLine($"Title not found for {repo.DocLinkSettings.RelativeLinkPrefix}{doc.Key.Replace("./", string.Empty)} in PR #{pr.PrNumber}");
                                prs += $"#{pr.PrNumber}";
                            }
                        }
                        if (docLink == null)
                        {
                            // root directory:
                            docLink = isRootDirectoryArea ? 
                                $"[Title not found in: {prs}]({doc.Key.Replace("./", string.Empty)})" :
                                $"[Title not found in: {prs}]({repo.DocLinkSettings.RelativeLinkPrefix}{doc.Key.Replace("./", string.Empty)})";
                        }

                        if (!string.IsNullOrEmpty(docLink))
                        {
                            stream.Write($"- {docLink}");

                            if (isNew || repo.InclusionCriteria.OmitPullRequestTitles)
                            {
                                stream.WriteLine();
                            }
                            else
                            {
                                for (int prIndex = 0; prIndex < docPullRequests.Count; prIndex++)
                                {
                                    if (prIndex == 0 && docPullRequests.Count > 1)
                                        stream.WriteLine();

                                    var (_, _, PrTitle, PrNumber) = docPullRequests[prIndex];
                                    int prTitleLeftPadding = docPullRequests.Count > 1 ? 2 : 1;
                                    var trimmedPrTitle = $"- {PrTitle.Trim()}";
                                    int paddedTitleLength = prTitleLeftPadding + trimmedPrTitle.Length;
                                    stream.WriteLine(trimmedPrTitle.PadLeft(paddedTitleLength));
                                }
                            }
                        }
                    }

                    stream.WriteLine();
                }
            }

            string? getDocLink(string source, string docUrl, bool isRootDirectoryArea)
            {
                var path = Path.Combine(_configuration.PathToRepoRoot, source);
                if (!File.Exists(path))
                    return null;

                string metadataValue = (repo.DocLinkSettings.LinkFormat == LinkFormat.Xref)
                    ? RawContentFromLocalFile.RetrieveUidFromFile(path)
                    : RawContentFromLocalFile.RetrieveTitleFromFile(path);

                string? docLink = (repo.DocLinkSettings.LinkFormat, string.IsNullOrEmpty(metadataValue), isRootDirectoryArea) switch
                {
                    (_, true, _) => null, // Matches an invalid enum value. Potential bug, but tested at app load (during JSON schema validation).
                    (LinkFormat.Relative, false, true) => $"[{metadataValue.Replace("\"", string.Empty)}]({docUrl.Replace("./", string.Empty)})",
                    (LinkFormat.Xref, false, _) => $"<xref:{metadataValue}>",
                    (_, false, _) =>  $"[{metadataValue.Replace("\"", string.Empty)}]({repo.DocLinkSettings.RelativeLinkPrefix}{docUrl.Replace("./", string.Empty)})",
                };

                if (repo.DocLinkSettings.LinkFormat == LinkFormat.SiteRelative)
                    _fileExtensions.ForEach(extension => docLink = docLink?.Replace(extension, string.Empty));

                return docLink;
            }
        }
    }

    private async Task WriteContributorInformation(TextWriter stream)
    {
        var allContributors = from c in _contributors
                              orderby c.login
                              group c by (c.login, c.name) into stats
                              select (User: stats.Key, Count: stats.Count());

        if (allContributors.Any())
        {
            var sb = new StringBuilder();
            sb.AppendLine("## Community contributors");
            sb.AppendLine();
            sb.AppendLine(@$"The following people contributed to the {_configuration.Repository.DocSetProductName} docs during this period. Thank you! Learn how to contribute by following the links under ""Get involved"" in the [what's new landing page](index.yml).");
            sb.AppendLine();

            foreach (var (user, count) in allContributors.OrderByDescending(c => c.Count))
            {
                // Format a merged pull requests badge, for example:
                // https://img.shields.io/badge/Merged%20Pull%20Requests-7-green
                var altText = $"There were {count} pull requests merged by {user.name}.";
                var mergedPullRequestsBadge = $"![{altText}](https://img.shields.io/badge/Merged%20Pull%20Requests-{count}-green)";
                
                sb.AppendLine($"- [{user.login}](https://github.com/{user.login}){(user.name != null ? " - " + user.name : "")} {mergedPullRequestsBadge}");
            }

            await stream.WriteAsync(sb.ToString());
        }
    }

    private async Task ProcessPullRequests()
    {
        var repo = _configuration.Repository;
        var client = _configuration.GitHubClient;
        var ospoClient = _configuration.OspoClient;

        await processPRs();

        // If processing a private repo, fetch the PRs & community contributors from
        // the accompanying public repo. Merge private results with public results.
        if (repo.IsPrivateRepo)
        {
            repo.Name = repo.Name.Replace(PrivateRepoNameSuffix, string.Empty);
            await processPRs();
        }

        async Task processPRs()
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"== {repo.Owner}/{repo.Name} ({repo.Branch}) ==", Console.ForegroundColor);
            Console.ForegroundColor = ConsoleColor.Gray;

            var excludedContributors = new List<string>();
            var query = new PullRequestsMergedInSprint(
                client, repo.Owner, repo.Name, repo.Branch, repo.InclusionCriteria.Labels, _configuration.DateRange);
            var authorLoginFTECache = new Dictionary<string, bool?>();

            await foreach (var item in query.PerformQuery())
            {
                var prNumber = item.Number;
                Console.WriteLine($"Processing PR {prNumber}");

                if (!authorLoginFTECache.TryGetValue(item.Author.Login, out var isFTE))
                {
                    isFTE = await item.Author.IsMicrosoftFTE(ospoClient);
                    authorLoginFTECache[item.Author.Login] = isFTE;
                }

                if (isFTE == false)
                    _contributors.Add((login: item.Author.Login, name: item.Author.Name));
                else if (isFTE == true)
                    // If a user account was deleted, it's replaced with the "ghost" account.
                    // For example, https://github.com/MicrosoftDocs/visualstudio-docs/pull/5837.
                    excludedContributors.Add(!string.IsNullOrEmpty(item.Author.Login) ? item.Author.Login : "ghost");

                await ProcessSinglePullRequest(client, prNumber, item.Title, item.ChangedFiles, repo);
            }

            Console.WriteLine();
            var distinctExcludedContributors = excludedContributors.Distinct().OrderBy(c => c).ToList();
            var excludedContributorsCount = distinctExcludedContributors.Count;
            Console.WriteLine($"Excluded {excludedContributorsCount} {(excludedContributorsCount == 1 ? "contributor" : "contributors")}");
            for (int index = 1; index <= excludedContributorsCount; index++)
            {
                Console.WriteLine($"{index}. {distinctExcludedContributors[index - 1]}");
            }
            Console.WriteLine();
        }
    }

    private async Task ProcessSinglePullRequest(
        IGitHubClient client, int prNumber, string prTitle, int changedFiles, RepositoryDetail repo)
    {
        // Before sending the request to GitHub, check rules to minimize work and network requests.

        // 1. PRs with a number of files exceeding the value of `MaxFilesChanged` can be ignored.
        if (changedFiles > repo.InclusionCriteria.MaxFilesChanged)
        {
            Console.WriteLine($"Ignoring PR {prNumber}: {prTitle}");
            return;
        }

        // 2. PRs whose titles match the provided regex(es) can be ignored.
        foreach (var pattern in repo.InclusionCriteria.PullRequestTitlesToIgnore)
        {
            if (Regex.IsMatch(prTitle, pattern))
            {
                Console.WriteLine($"Ignoring PR {prNumber}: {prTitle}");
                return;
            }
        }

        var request = new PullRequestFilesRequest(client, repo.Owner, repo.Name, prNumber);
        if (await request.PerformQueryAsync())
        {
            foreach (var prFile in request.Files)
            {
                var path = prFile.Filename.ToLower();
                var extension = Path.GetExtension(path);
                var baseFileName = Path.GetFileNameWithoutExtension(path);

                // The file must begin with the path defined in the config file's `rootDirectory` property.
                var notInclude = path.StartsWith(repo.RootDirectory) &&
                    !path.Contains("/includes/") &&
                    baseFileName != "license" &&
                    baseFileName != "readme" &&
                    baseFileName != "toc";

                // Build the conditions to examine
                var useFile = _fileExtensions.Contains(extension);
                useFile &= notInclude;

                var significant = prFile.Status switch
                {
                    PullRequestFilesRequest.FileStatus.Added => true,
                    PullRequestFilesRequest.FileStatus.Modified =>
                        prFile.Additions >= repo.InclusionCriteria.MinAdditionsToFile,
                    _ => false,
                };

                if (useFile && significant)
                {
                    PrDetail prDetail = new (prFile.Filename,
                                    prFile.Status is PullRequestFilesRequest.FileStatus.Added,
                                    prTitle,
                                    prNumber);
                    var link = path.Replace(repo.RootDirectory, string.Empty);

                    // If the link doesn't contain a slash, it must reside in the root directory.
                    // Prepend the string with a "./" to indicate so.
                    if (!link.Contains('/'))
                        link = $"./{link}";

                    if (!_majorChanges.ContainsKey(link))
                    {
                        _majorChanges[link] =
                            new List<PrDetail> { { prDetail } };
                    }
                    else
                    {
                        _majorChanges[link].Add(prDetail);
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"Ignoring {prNumber}: {prTitle} due to error {request.ErrorMessage}");
            return;
        }
    }
}
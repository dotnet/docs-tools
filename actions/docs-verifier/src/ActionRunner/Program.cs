using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using GitHub;
using MarkdownLinksVerifier;
using MarkdownLinksVerifier.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Octokit;
using RedirectionVerifier;

ConfigurationReader configurationReader = new();
MarkdownLinksVerifierConfiguration? configuration = await configurationReader.ReadConfigurationAsync();

Console.WriteLine($"Starting Markdown Links Verifier in '{Directory.GetCurrentDirectory()}'.");
List<LinkError> result = await MarkdownFilesAnalyzer.GetResultsAsync(configuration);

foreach (LinkError linkError in result)
{
    Console.WriteLine($"::error::In file '{linkError.File}': Invalid link: '{linkError.Link}'.");
}

int returnCode = result.Count;

// Get all redirection files.
ImmutableArray<string> redirectionFiles = await RedirectionHelpers.GetRedirectionFileNames();

var allRedirections = new List<Redirection>();
if (!redirectionFiles.IsDefault)
{
    foreach (string redirectionFile in redirectionFiles)
    {
        OpenPublishingRedirectionReader redirectionReader = new(redirectionFile);
        ImmutableArray<Redirection> redirections = await redirectionReader.MapConfigurationAsync();
        if (!redirections.IsDefault)
            allRedirections.AddRange(redirections);
    }
}

if (Environment.GetEnvironmentVariable("IS_TRY_FIX") is "true")
{
    //await File.WriteAllTextAsync("Hello_from_actions.md", "Hello!");
    foreach (LinkError linkError in result)
    {
        // For every reported invalid link, check if it already has a redirection so that it can be fixed automatically.
        string sourcePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), linkError.AbsolutePath);
        Redirection? redirection = allRedirections.SingleOrDefault(redirection => redirection.MatchesSourcePath(sourcePath));
        const string dotnetPrefix = "/dotnet/";
        if (redirection is { RedirectUrl: string redirectUrl } && redirectUrl.StartsWith(dotnetPrefix, StringComparison.Ordinal))
        {
            var newAbsolutePath = $"docs/{redirectUrl[dotnetPrefix.Length..]}";
            if (File.Exists($"{newAbsolutePath}.md"))
            {
                newAbsolutePath += ".md";
            }
            else if (File.Exists($"{newAbsolutePath}.yml"))
            {
                newAbsolutePath += ".yml";
            }
            else
            {
                continue;
            }

            string file = await File.ReadAllTextAsync(linkError.File);
            file = file.Remove(linkError.UrlSpan.Start, linkError.UrlSpan.Length);

            string newLink = Path.GetRelativePath(Path.GetDirectoryName(linkError.File)!, newAbsolutePath);
            int queryIndex = linkError.Link.LastIndexOf('?');
            int headingIndex = linkError.Link.LastIndexOf('#');
            int queryOrHeadingIndex = (queryIndex, headingIndex) switch
            {
                // No query or heading.
                (-1, -1) => -1,

                // If we have both '#' and '?', we want to get them both.
                // So select the minimum index.
                ( > -1, > -1) => Math.Min(queryIndex, headingIndex),

                // We have one of '#' and '?'
                _ => Math.Max(queryIndex, headingIndex),
            };

            if (queryOrHeadingIndex > -1)
            {
                newLink += linkError.Link.Substring(queryOrHeadingIndex);
            }

            file = file.Insert(linkError.UrlSpan.Start, newLink);
            await File.WriteAllTextAsync(linkError.File, file);
            Console.WriteLine($"CAN FIX: '{sourcePath}' to '{newAbsolutePath}'");
        }
    }
    return 0;

}

// on: pull_request
// env:
//   GITHUB_PR_NUMBER: ${{ github.event.pull_request.number }}
if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER"), out int pullRequestNumber))
{
    throw new InvalidOperationException($"The value of GITHUB_PR_NUMBER environment variable is not valid.");
}

DocfxConfigurationReader docfxConfigurationReader = new();
IEnumerable<Matcher> matchers = await docfxConfigurationReader.MapConfigurationAsync();
IEnumerable<PullRequestFile> pullRequestFiles = await GitHubPullRequest.GetPullRequestFilesAsync(pullRequestNumber);

WhatsNewConfigurationReader whatsNewConfigurationReader = new();
string? whatsNewPath = await whatsNewConfigurationReader.MapConfigurationAsync();

List<PullRequestFile> files =
    pullRequestFiles.Where(f => IsRedirectableFile(f, matchers, whatsNewPath)).ToList();

// We should only ever fail on MD and YML files, no other files require redirection.
// Also, filter out files that are part of the "What's new" directory - as they shouldn't require redirects.
foreach (PullRequestFile file in files)
{
    // Changing the extension from .yml to .md or the opposite doesn't require a redirection.
    // In both cases, the URL in live docs site is the same.
    if (file.IsRenamed() && !IsExtensionChangeOnly(file.PreviousFileName, file.FileName))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.PreviousFileName, allRedirections))
        {
            returnCode++;
        }
    }
    else if (file.IsRemoved() && !files.Any(f => f.IsAdded() && IsExtensionChangeOnly(file.FileName, f.FileName)))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.FileName, allRedirections))
        {
            returnCode++;
        }
    }
}

return returnCode;

static bool IsRedirectableFile(
    PullRequestFile file, IEnumerable<Matcher> matchers, string? whatsNewPath)
{
    string? deletedFileName = file.IsRenamed()
        ? file.PreviousFileName
        : (file.IsRemoved() ? file.FileName : null);

    bool isDeletedToc = deletedFileName is not null
        && deletedFileName.EndsWith("toc.yml", StringComparison.OrdinalIgnoreCase);

    // A deleted toc.yml doesn't need redirection.
    // Also, don't require a redirection for file patterns specified as "exclude"s in docfx config file.
    return !isDeletedToc && IsYmlOrMarkdownFile(deletedFileName)
        && !IsInWhatsNewDirectory(deletedFileName, whatsNewPath) &&
        matchers.Any(m => m.Match(deletedFileName).HasMatches);
}

static bool IsYmlOrMarkdownFile([NotNullWhen(true)] string? fileName) =>
    Path.GetExtension(fileName) is ".yml" or ".md";

static bool IsInWhatsNewDirectory(string fileName, string? whatsNewPath)
{
    if (whatsNewPath is { Length: > 0 })
    {
        // Example:
        // file.FileName:   docs/whats-new/2021-03.md
        // whatsNewPath:    docs/whats-new

        return fileName.StartsWith(whatsNewPath, StringComparison.OrdinalIgnoreCase);
    }

    return false;
}

static bool IsExtensionChangeOnly(string file1, string file2) =>
    RemoveExtension(file1).Equals(RemoveExtension(file2), StringComparison.OrdinalIgnoreCase);

static string RemoveExtension(string file) =>
    file.Substring(0, file.Length - Path.GetExtension(file).Length);

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub;
using MarkdownLinksVerifier;
using MarkdownLinksVerifier.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;
using Octokit;
using RedirectionVerifier;

MarkdownLinksVerifierConfiguration configuration = await ConfigurationReader.GetConfigurationAsync();
int returnCode = await MarkdownFilesAnalyzer.WriteResultsAsync(Console.Out, configuration);

// on: pull_request
// env:
//   GITHUB_PR_NUMBER: ${{ github.event.pull_request.number }}
if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER"), out int pullRequestNumber))
{
    throw new InvalidOperationException($"The value of GITHUB_PR_NUMBER environment variable is not valid.");
}

List<PullRequestFile> files = (await GitHubPullRequest.GetPullRequestFilesAsync(pullRequestNumber)).Where(f => IsRedirectableFile(f)).ToList();

// We should only ever fail on MD and YML files, no other files require redirection.
// Also, filter out files that are part of the "What's new" directory - as they shouldn't require redirects.
foreach (PullRequestFile file in files)
{
    Matcher? matcher = DocfxConfigurationReader.GetMatcher();
    // Changing the extension from .yml to .md or the opposite doesn't require a redirection.
    // In both cases, the URL in live docs site is the same.
    if (file.IsRenamed() && matcher?.Match(file.PreviousFileName)?.HasMatches == true && !IsExtensionChangeOnly(file.PreviousFileName, file.FileName))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.PreviousFileName))
        {
            returnCode++;
        }
    }
    else if (file.IsRemoved() && matcher?.Match(file.FileName)?.HasMatches == true  && !files.Any(f => f.IsAdded() && IsExtensionChangeOnly(file.FileName, f.FileName)))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.FileName))
        {
            returnCode++;
        }
    }
}

return returnCode;

static bool IsRedirectableFile(PullRequestFile file)
{
    string? deletedFileName = file.IsRenamed()
        ? file.PreviousFileName
        : (file.IsRemoved() ? file.FileName : null);

    bool isDeletedToc = deletedFileName is not null && deletedFileName.Equals("toc.yml", StringComparison.OrdinalIgnoreCase);
    // A deleted toc.yml doesn't need redirection.
    return !isDeletedToc && IsYmlOrMarkdownFile(deletedFileName) && !IsInWhatsNewDirectory(deletedFileName);
}

static bool IsYmlOrMarkdownFile(string? fileName) => Path.GetExtension(fileName) is ".yml" or ".md";

static bool IsInWhatsNewDirectory(string? fileName)
{
    string? whatsNewPath = WhatsNewConfigurationReader.GetWhatsNewPath();
    if (whatsNewPath is { Length: > 0 })
    {
        // Example:
        // file.FileName:   docs/whats-new/2021-03.md
        // whatsNewPath:    docs/whats-new

        return fileName?.StartsWith(whatsNewPath, StringComparison.OrdinalIgnoreCase) == true;
    }

    return false;
}

static bool IsExtensionChangeOnly(string file1, string file2) =>
    RemoveExtension(file1).Equals(RemoveExtension(file2), StringComparison.OrdinalIgnoreCase);

static string RemoveExtension(string file) =>
    file.Substring(0, file.Length - Path.GetExtension(file).Length);

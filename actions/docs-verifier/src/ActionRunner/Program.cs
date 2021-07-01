using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub;
using MarkdownLinksVerifier;
using MarkdownLinksVerifier.Configuration;
using Octokit;
using RedirectionVerifier;

MarkdownLinksVerifierConfiguration configuration = await ConfigurationReader.GetConfigurationAsync();
var returnCode = await MarkdownFilesAnalyzer.WriteResultsAsync(Console.Out, configuration);

// on: pull_request
// env:
//   GITHUB_PR_NUMBER: ${{ github.event.pull_request.number }}
if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER"), out int pullRequestNumber))
{
    throw new InvalidOperationException($"The value of GITHUB_PR_NUMBER environment variable is not valid.");
}

static bool IsYmlOrMarkdownFile(PullRequestFile file) => Path.GetExtension(file.FileName) is ".yml" or ".md";

static bool IsInWhatsNewDirectory(PullRequestFile file)
{
    string? whatsNewPath = WhatsNewConfigurationReader.GetWhatsNewPath();
    if (whatsNewPath is { Length: > 0 })
    {
        // Example:
        // file.FileName:   docs/whats-new/2021-03.md
        // whatsNewPath:    docs/whats-new

        return file.FileName.StartsWith(whatsNewPath, StringComparison.OrdinalIgnoreCase);
    }

    return false;
}

List<PullRequestFile> files = (await GitHubPullRequest.GetPullRequestFilesAsync(pullRequestNumber)).ToList();

// We should only ever fail on MD and YML files, no other files require redirection.
// Also, filter out files that are part of the "What's new" directory - as they shouldn't require redirects.
foreach (PullRequestFile file in files.Where(f => IsYmlOrMarkdownFile(f) && !IsInWhatsNewDirectory(f)))
{
    // Changing the extension from .yml to .md or the opposite doesn't require a redirection.
    // In both cases, the URL in live docs site is the same.
    if (file.IsRenamed() && !IsExtensionChangeOnly(file.PreviousFileName, file.FileName))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.PreviousFileName))
        {
            returnCode++;
        }
    }
    else if (file.IsRemoved() && !files.Any(f => f.IsAdded() && IsExtensionChangeOnly(file.FileName, f.FileName)))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.FileName))
        {
            returnCode++;
        }
    }

    static bool IsExtensionChangeOnly(string file1, string file2) =>
        TryStripYmlOrMarkdownExtension(file1, out string strippedFile1) &&
        TryStripYmlOrMarkdownExtension(file2, out string strippedFile2) &&
        strippedFile1.Equals(strippedFile2, StringComparison.OrdinalIgnoreCase);

    static bool TryStripYmlOrMarkdownExtension(string file, out string strippedFile)
    {
        (string subFile, bool result) = Path.GetExtension(file) switch
        {
            string ext when ext is ".yml" or ".md" =>
                (file.Substring(0, file.Length - ext.Length), true),
            _ => (file, false)
        };

        strippedFile = subFile;
        return result;
    }
}

return returnCode;

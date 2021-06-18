using System;
using System.Collections.Generic;
using System.Linq;
using GitHub;
using MarkdownLinksVerifier;
using MarkdownLinksVerifier.Configuration;
using Octokit;
using RedirectionVerifier;

var returnCode = 0;
MarkdownLinksVerifierConfiguration configuration = await ConfigurationReader.GetConfigurationAsync();
returnCode += await MarkdownFilesAnalyzer.WriteResultsAsync(Console.Out, configuration);

// on: pull_request
// env:
//   GITHUB_PR_NUMBER: ${{ github.event.pull_request.number }}
if (!int.TryParse(Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER"), out int pullRequestNumber))
{
    throw new InvalidOperationException($"The value of GITHUB_PR_NUMBER environment variable is not valid.");
}

List<PullRequestFile> files = (await GitHubPullRequest.GetPullRequestFilesAsync(pullRequestNumber)).ToList();

foreach (PullRequestFile file in files)
{
    if (file.IsRenamed() && file.PreviousFileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.PreviousFileName))
        {
            returnCode++;
        }
    }
    else if (file.IsRemoved() && file.FileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
    {
        if (!await RedirectionsVerifier.WriteResultsAsync(Console.Out, file.FileName))
        {
            returnCode++;
        }
    }
}

return returnCode;

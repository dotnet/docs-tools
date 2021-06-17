using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using Xunit;

namespace GitHub.UnitTests
{
    public class PullRequestFileExtensionsTests
    {
        [Fact]
        public async Task TestExtensions()
        {
            var client = new GitHubClient(new ProductHeaderValue("my-cool-app"));
            List<PullRequestFile> files = (await client.PullRequest.Files("dotnet", "docs", 23395).ConfigureAwait(false)).ToList();

            PullRequestFile addedFile = files.Single(f => f.FileName == ".github/workflows/markdown-links-verifier.yml");
            Assert.True(addedFile.IsAdded());
            Assert.False(addedFile.IsRenamed());
            Assert.False(addedFile.IsRemoved());
            Assert.Null(addedFile.PreviousFileName);
            Assert.NotNull(addedFile.FileName);

            PullRequestFile modifiedFile = files.Single(f => f.FileName.EndsWith("containerized-lifecycle/Microsoft-platform-tools-containerized-apps/index.md", StringComparison.Ordinal));
            Assert.False(modifiedFile.IsAdded());
            Assert.False(modifiedFile.IsRenamed());
            Assert.False(addedFile.IsRemoved());
            Assert.Null(modifiedFile.PreviousFileName);
            Assert.NotNull(modifiedFile.FileName);

            PullRequestFile renamedFile = files.Single(f => f.FileName.EndsWith("ic-end-to-enddpcker-app-life-cycle.png", StringComparison.Ordinal));
            Assert.False(renamedFile.IsAdded());
            Assert.True(renamedFile.IsRenamed());
            Assert.False(addedFile.IsRemoved());
            Assert.NotNull(renamedFile.PreviousFileName);
            Assert.NotNull(renamedFile.FileName);
        }

        [Fact]
        public async Task TestRemovedFile()
        {
            var client = new GitHubClient(new ProductHeaderValue("my-cool-app"));
            List<PullRequestFile> files = (await client.PullRequest.Files("dotnet", "docs", 24546).ConfigureAwait(false)).ToList();

            PullRequestFile removedfile = files.Single(f => f.FileName == ".github/workflows/markdown-links-verifier.yml");
            Assert.False(removedfile.IsAdded());
            Assert.False(removedfile.IsRenamed());
            Assert.True(removedfile.IsRemoved());
            Assert.Null(removedfile.PreviousFileName);
            Assert.NotNull(removedfile.FileName);

        }
    }
}

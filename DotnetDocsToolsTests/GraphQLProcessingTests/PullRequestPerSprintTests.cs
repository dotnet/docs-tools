using DotNet.DocsTools.GraphQLQueries;
using DotNet.DocsTools.Utility;
using System.Text.Json;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests
{
    public class PullRequestPerSprintTests
    {
        private readonly SprintDateRange dates = SprintDateRange.GetSprintFor(DateTime.Now);

        private readonly string lastPageReponse =
@"{
  ""data"": {
    ""search"": {
      ""pageInfo"": {
        ""hasNextPage"": false,
        ""endCursor"": null
      },
      ""nodes"": [
        {
          ""title"": ""CodeDom small improvements"",
          ""number"": 16410,
          ""changedFiles"": 3,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3MDU2ODU1"",
          ""author"": {
            ""login"": ""gewarren"",
            ""name"": ""Genevieve Warren""
          }
        },
        {
          ""title"": ""Update wcf-services-and-aspnet.md"",
          ""number"": 16406,
          ""changedFiles"": 1,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3MDI5MDgy"",
          ""author"": {
            ""login"": ""Thraka"",
            ""name"": ""Andy De George""
          }
        },
        {
          ""title"": ""Acrolinx 12/26"",
          ""number"": 16405,
          ""changedFiles"": 3,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3MDIzMzIx"",
          ""author"": {
            ""login"": ""gewarren"",
            ""name"": ""Genevieve Warren""
          }
        }
      ]
    }
  }
}";

        private readonly string firstPageResponse =
@"{
  ""data"": {
    ""search"": {
      ""pageInfo"": {
        ""hasNextPage"": true,
        ""endCursor"": ""Y3Vyc29yOjM""
    },
      ""nodes"": [
        {
          ""title"": ""Fix C# and VB code snippets"",
          ""number"": 16433,
          ""changedFiles"": 1,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3NTQ4Mjgx"",
          ""author"": {
            ""login"": ""Youssef1313"",
            ""name"": ""Youssef Victor""
          }
        },
        {
          ""title"": ""Replace Windows Server 2008 tokens"",
          ""number"": 16416,
          ""changedFiles"": 14,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3MjY4MjE1"",
          ""author"": {
            ""login"": ""NextTurn"",
            ""name"": ""Next Turn""
          }
        },
        {
          ""title"": ""Fix WPF TOC for F1 help release"",
          ""number"": 16412,
          ""changedFiles"": 1,
          ""id"": ""MDExOlB1bGxSZXF1ZXN0MzU3MDg1MDI1"",
          ""author"": {
            ""login"": ""Thraka"",
            ""name"": ""Andy De George""
          }
        }
      ]
    }
  }
}";

        [Fact]
        public async Task CanEnumerateASinglePageResponse()
        {
            var responseDoc = JsonDocument.Parse(lastPageReponse);
            var client = new FakeGitHubClient(responseDoc);

            var mergedPRs = new PullRequestsMergedInSprint(
                client, "dotnet", "docs", "main", null, new DateRange(dates.StartDate, dates.EndDate));

            var count = 0;
            await foreach(var pr in mergedPRs.PerformQuery())
            {
                Assert.True(pr.ChangedFiles > 0);
                Assert.True(pr.Number > 0);
                Assert.False(string.IsNullOrWhiteSpace(pr.Title));
                Assert.False(string.IsNullOrWhiteSpace(pr.Author.Login));

                count++;
            }
            Assert.Equal(3, count);
        }

        [Fact]
        public async Task CanEnumerateAMultiPageResponse()
        {
            var responseDocFirst = JsonDocument.Parse(firstPageResponse);
            var responseDocLast = JsonDocument.Parse(lastPageReponse);
            var client = new FakeGitHubClient(responseDocFirst, responseDocLast);

            var mergedPRs = new PullRequestsMergedInSprint(client, "dotnet", "docs", "main", null, new DateRange(dates.StartDate, dates.EndDate));

            var count = 0;
            await foreach (var pr in mergedPRs.PerformQuery())
            {
                Assert.True(pr.ChangedFiles > 0);
                Assert.True(pr.Number > 0);
                Assert.False(string.IsNullOrWhiteSpace(pr.Title));
                Assert.False(string.IsNullOrWhiteSpace(pr.Author.Login));

                count++;
            }
            Assert.Equal(6, count);
        }

        [Fact]
        public void GitHubClientMustNotBeNull() =>
            Assert.Throws<ArgumentNullException>(() => new PullRequestsMergedInSprint(
                default!, 
                "dotnet", 
                "docs", 
                "main",
                null,
                new DateRange(dates.StartDate, dates.EndDate)));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void OrgMustBeNonWhitespace(string orgName) =>
            Assert.Throws<ArgumentException>(() => new PullRequestsMergedInSprint(
                new FakeGitHubClient(),
                orgName,
                "docs",
                "main",
                null,
                new DateRange(dates.StartDate, dates.EndDate)));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void RepositoryMustBeNonWhitespace(string repoName) =>
            Assert.Throws<ArgumentException>(() => new PullRequestsMergedInSprint(
                new FakeGitHubClient(),
                "dotnet",
                repoName,
                "main",
                null,
                new DateRange(dates.StartDate, dates.EndDate)));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void BranchMustBeNonWhitespace(string branchName) =>
            Assert.Throws<ArgumentException>(() => new PullRequestsMergedInSprint(
                new FakeGitHubClient(),
                "dotnet",
                "docs",
                branchName,
                null,
                new DateRange(dates.StartDate, dates.EndDate)));
    }
}

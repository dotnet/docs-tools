using DotnetDocsTools.GraphQLQueries;
using System.Text.Json;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests
{
    public class FilesInPrTests
    {
        private readonly string singlePageREsponse =
@"{
  ""data"": {
    ""repository"": {
      ""pullRequest"": {
        ""files"": {
          ""pageInfo"": {
            ""hasNextPage"": false,
            ""endCursor"": ""MQ""
          },
          ""nodes"": [
            {
              ""path"": ""snippets/csharp/VS_Snippets_VBCSharp/CsLINQJoin/CS/JoinOperation.cs""
            }
          ]
        }
      }
    }
  }
}";

        [Fact]
        public async Task CanEnumerateASinglePageResponse()
        {
            var responseDoc = JsonDocument.Parse(singlePageREsponse);
            var client = new FakeGitHubClient(responseDoc);

            var filesInPR = new FilesInPullRequest(client, "dotnet", "samples", 1876);

            var count = 0;
            await foreach(var path in filesInPR.PerformQuery())
            {
                Assert.False(string.IsNullOrWhiteSpace(path));
                count++;
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public void GitHubClientMustNotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new FilesInPullRequest(
                default!, 
                "dotnet", 
                "samples", 
                1876));
        }

        [Fact]
        public void OrgMustBeNonWhitespace()
        {
            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                null!,
                "samples",
                1876));

            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                "",
                "samples",
                1876));
            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                "     ",
                "samples",
                1876));
        }

        [Fact]
        public void RepositoryMustBeNonWhitespace()
        {
            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                "dotnet",
                null!,
                1876));

            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                "dotnet",
                "",
                1876));
            Assert.Throws<ArgumentException>(() => new FilesInPullRequest(
                new FakeGitHubClient(),
                "dotnet",
                "     ",
                1876));
        }
    }
}

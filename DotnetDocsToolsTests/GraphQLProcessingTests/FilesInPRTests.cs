using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;
using Xunit;

namespace DotNetDocs.Tools.Tests.GraphQLProcessingTests;

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
        var count = 0;

        var filesQuery = new EnumerationQuery<PullRequestFiles, FilesModifiedVariables>(client);

        await foreach (var item in filesQuery.PerformQuery(new FilesModifiedVariables("dotnet", "samples", 1876)))
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Path));
            count++;
        }
        Assert.Equal(1, count);
    }
}

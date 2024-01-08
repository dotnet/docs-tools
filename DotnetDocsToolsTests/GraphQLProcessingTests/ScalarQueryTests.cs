using DotNet.DocsTools.GitHubObjects;
using DotNetDocs.Tools.GitHubCommunications;
using DotNetDocs.Tools.GraphQLQueries;
using System.Text.Json;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests;

public class ScalarQueryTests
{
    private const string ValidResult = """
        {
          "data": {
            "repository": {
              "defaultBranchRef": {
                "name": "main"
              }
            }
          }
        }
        """;

    private class FakeClient : IGitHubClient
    {
        public void Dispose() { }

        public Task<JsonDocument> GetReposRESTRequestAsync(params string[] restPath)
        {
            throw new NotImplementedException();
        }

        public Task<JsonElement> PostGraphQLRequestAsync(GraphQLPacket queryText) =>
            Task.FromResult(JsonDocument.Parse(ValidResult).RootElement.GetProperty("data"));
    }

    [Fact]
    public async Task ScalarQueryNavigationIsCorrect()
    {
        using FakeClient client = new();
        var query = new ScalarQuery<DefaultBranch, DefaultBranchVariables>(client);
        var result = await query.PerformQuery(new DefaultBranchVariables("dotnet","docs"));
        Assert.Equal("main", result?.DefaultBranchName);
    }
}
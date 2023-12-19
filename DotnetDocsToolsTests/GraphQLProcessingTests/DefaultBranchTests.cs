using DotNet.DocsTools.GitHubObjects;
using System.Text.Json;
using Xunit;

namespace DotnetDocsTools.Tests.GraphQLProcessingTests;

public class DefaultBranchTests
{
    private const string ValidResult = """
        {
            "name": "main"
        }
        """;


    [Fact]
    public void DefaultBranchName_Is_Correct()
    {
        var variables = new DefaultBranchVariables
        {
            Organization = "dotnet",
            Repository = "docs"
        };

        JsonElement element = JsonDocument.Parse(ValidResult).RootElement;
        var issue = DefaultBranch.FromJsonElement(element, variables);
    }
}

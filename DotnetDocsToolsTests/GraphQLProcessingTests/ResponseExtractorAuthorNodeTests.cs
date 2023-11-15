using System.Text.Json;
using Xunit;
using DotNet.DocsTools.GitHubObjects;

namespace DotnetDocsTools.Tests;

public class ResponseExtractorAuthorNodeTests
{

    [Fact]
    public void ParserExtractsValidNameFromAuthorNodeWithName()
    {
        const string validNodeWithName = """
        {
           "login": "billWagner",
           "name": "Bill Wagner"
        }
        """;

        JsonElement element = JsonDocument.Parse(validNodeWithName).RootElement;
        Assert.Equal("Bill Wagner", ResponseExtractors.NameFromAuthorNode(element));
    }

    [Fact]
    public void ParserExtractsLoginFromAuthorNodeWithPrivateName()
    {
        const string validNodeWithoutName = """
        {
           "login": "billWagner",
           "name": null
        }
        """;

    JsonElement element = JsonDocument.Parse(validNodeWithoutName).RootElement;
        Assert.Equal("billWagner", ResponseExtractors.LoginFromAuthorNode(element));
        Assert.Equal(string.Empty, ResponseExtractors.NameFromAuthorNode(element));
    }

    [Fact]
    public void ParserFailsWhenGivenWrongNode()
    {
        const string parentNode = """
        {
          "author": {
             "login": "billWagner",
             "name": null
          }
        }
        """;

        JsonElement element = JsonDocument.Parse(parentNode).RootElement;
        Assert.Throws<ArgumentException>(() => ResponseExtractors.LoginFromAuthorNode(element));
        Assert.Throws<ArgumentException>(() =>ResponseExtractors.NameFromAuthorNode(element));
    }

    [Fact]
    public void ParserReportsErrorOnWrongNode()
    {
        JsonElement element = JsonDocument.Parse("null").RootElement;
        Assert.Throws<ArgumentException>(() => ResponseExtractors.LoginFromAuthorNode(element));
        Assert.Throws<ArgumentException>(() => ResponseExtractors.NameFromAuthorNode(element));
    }
}

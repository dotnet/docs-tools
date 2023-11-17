using System.Text.Json;
using Xunit;
using DotNet.DocsTools.GitHubObjects;

namespace DotnetDocsTools.Tests;

public class ActorNodeTests
{
    [Fact]
    public void ActorCreatedFromValidNodeWithName()
    {
        const string validNodeWithName = """
        {
           "login": "billWagner",
           "name": "Bill Wagner"
        }
        """;

        JsonElement element = JsonDocument.Parse(validNodeWithName).RootElement;
        var actor = Actor.FromJsonElement(element);
        Assert.NotNull(actor);
        Assert.Equal("billWagner", actor?.Login);
        Assert.Equal("Bill Wagner", actor?.Name);
    }

    [Fact]
    public void ActorCreatedFromValidPrivateNameNode()
    {
        const string validNodeWithoutName = """
        {
           "login": "billWagner",
           "name": null
        }
        """;

        JsonElement element = JsonDocument.Parse(validNodeWithoutName).RootElement;
        var actor = Actor.FromJsonElement(element);
        Assert.NotNull(actor);
        Assert.Equal("billWagner", actor?.Login);
        Assert.Empty(actor!.Name);
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
        Assert.Throws<ArgumentException>(() => Actor.FromJsonElement(element));
    }

    [Fact]
    public void NullActorFromNullNode()
    {
        JsonElement element = JsonDocument.Parse("null").RootElement;
        var actor = Actor.FromJsonElement(element);
        Assert.Null(actor);
    }
}

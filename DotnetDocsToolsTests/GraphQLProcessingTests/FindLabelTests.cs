using DotNetDocs.Tools.GraphQLQueries;
using Xunit;

namespace DotNetDocs.Tools.Tests.GraphQLProcessingTests;

public class FindLabelTests
{
    // Not as many tests here, as the query has
    // been tested. I added the tests for argument
    // validation so that if we find issues later, it
    // will be easy to add tests that show where our
    // assumptions were wrong.

    [Fact]
    public void GitHubClientMustNotBeNull()
    {
        Assert.Throws<ArgumentNullException>(() => new FindLabelQuery(
            default!,
            "dotnet",
            "samples",
            "someLabel"));
    }

    [Fact]
    public void OrgMustBeNonWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            null!,
            "samples",
            "someLabel"));

        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "",
            "samples",
            "someLabel"));
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "     ",
            "samples",
            "someLabel"));
    }

    [Fact]
    public void RepositoryMustBeNonWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            null!,
            "someLabel"));

        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            "",
            "someLabel"));
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            "     ",
            "someLabel"));
    }

    [Fact]
    public void LabelTextMustBeNonWhitespace()
    {
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            "samples",
            default!));

        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            "samples",
            ""));
        Assert.Throws<ArgumentException>(() => new FindLabelQuery(
            new FakeGitHubClient(),
            "dotnet",
            "docs",
            "     "));
    }
}
